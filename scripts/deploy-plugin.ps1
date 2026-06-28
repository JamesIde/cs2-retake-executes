param(
    [Parameter(Mandatory = $true)]
    [string]$Plugin,
    [bool]$Restart = $false,
    [string]$ConfigFile = "$PSScriptRoot\..\deploy.config.json"
)

try {
    if (-not (Test-Path $ConfigFile)) {
        Write-Host "Config file not found at $ConfigFile" -ForegroundColor Red
        Write-Host "Copy deploy.config.template.json to deploy.config.json and fill in your values" -ForegroundColor Yellow
        exit 1
    }

    $config = Get-Content $ConfigFile | ConvertFrom-Json

    $required = @("ServerIp", "ServerPort", "SftpUser", "SftpPassword", "PluginsBasePath", "RemotePluginsPath")
    foreach ($field in $required) {
        if (-not $config.$field) {
            Write-Host "Missing required config field: $field" -ForegroundColor Red
            exit 1
        }
    }

    $ServerIp = $config.ServerIp
    $ServerPort = $config.ServerPort
    $SftpUser = $config.SftpUser
    $SftpPassword = $config.SftpPassword
    $PluginsBasePath = $config.PluginsBasePath
    $RemotePluginsPath = $config.RemotePluginsPath
    $BuildPath = "$PluginsBasePath\$Plugin\bin\Release\net8.0"
    $RemotePluginPath = "$RemotePluginsPath/$Plugin"

    # step 1 - build
    Write-Host "Building $Plugin..." -ForegroundColor Cyan
    Set-Location "$PluginsBasePath\$Plugin"
    dotnet build -c Release -f net8.0

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed - fix errors before deploying" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build succeeded" -ForegroundColor Green

    # step 2 - connect via FTP
    Add-Type -Path "C:\Program Files (x86)\WinSCP\WinSCPnet.dll"

    $sessionOptions = New-Object WinSCP.SessionOptions -Property @{
        Protocol                                     = [WinSCP.Protocol]::Ftp
        HostName                                     = $ServerIp
        PortNumber                                   = $ServerPort
        UserName                                     = $SftpUser
        Password                                     = $SftpPassword
        FtpSecure                                    = [WinSCP.FtpSecure]::Explicit
        GiveUpSecurityAndAcceptAnyTlsHostCertificate = $true
    }

    $session = New-Object WinSCP.Session

    try {
        Write-Host "Connecting to Streamline..." -ForegroundColor Cyan
        $session.Open($sessionOptions)
        Write-Host "Connected" -ForegroundColor Green

        # create plugin directory if it doesn't exist
        if (-not $session.FileExists($RemotePluginPath)) {
            Write-Host "Creating remote plugin directory..." -ForegroundColor Cyan
            $session.CreateDirectory($RemotePluginPath)
        }

        # step 3 - upload files
        $files = @("$Plugin.dll", "$Plugin.pdb", "$Plugin.deps.json", "Dapper.dll", "MySqlConnector.dll")

        foreach ($file in $files) {
            $localFile = "$BuildPath\$file"
            if (Test-Path $localFile) {
                Write-Host "Uploading $file..." -ForegroundColor Cyan
                $transferResult = $session.PutFiles($localFile, "$RemotePluginPath/$file")
                $transferResult.Check()
                Write-Host "Uploaded $file" -ForegroundColor Green
            }
            else {
                Write-Host "Warning: $file not found, skipping" -ForegroundColor Yellow
            }
        }

        Write-Host "Plugin deployed successfully" -ForegroundColor Green
    }
    finally {
        $session.Dispose()
    }

    # step 4 - reload via RCON
    if ($Restart) {
        $rconPath = "$PSScriptRoot\tools\rcon.exe"

        $rconAddress = "$($ServerIp):$($config.RconPort)"

        Write-Host "Reloading $Plugin via RCON..." -ForegroundColor Cyan
        & $rconPath -a $rconAddress -p $config.RconPassword "css_plugins unload $Plugin"
        Start-Sleep -Seconds 1
        & $rconPath -a $rconAddress -p $config.RconPassword "css_plugins load $Plugin"
        Write-Host "Plugin reloaded" -ForegroundColor Green
    }
    else {
        Write-Host "Skipping reload. Pass -Restart `$true to reload the plugin." -ForegroundColor Gray
    }
}
catch {
    Write-Host "Something went wrong: $_" -ForegroundColor Red
}
finally {
    Set-Location $PSScriptRoot
}