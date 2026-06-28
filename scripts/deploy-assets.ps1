param(
    [string]$Plugin = "RetakeExecutes",
    [string]$ConfigFile = "$PSScriptRoot\..\deploy.config.json"
)

try {
    # load config
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

    $MapsBasePath = "$PluginsBasePath\Maps"
    $RemoteMapsPath = "$RemotePluginsPath/$Plugin/maps"

    # check maps directory exists
    if (-not (Test-Path $MapsBasePath)) {
        Write-Host "Maps directory not found at $MapsBasePath" -ForegroundColor Red
        exit 1
    }

    # collect all map json files
    $mapFiles = Get-ChildItem -Path $MapsBasePath -Recurse -Filter "*.json"

    if ($mapFiles.Count -eq 0) {
        Write-Host "No map files found in $MapsBasePath" -ForegroundColor Yellow
        exit 0
    }

    Write-Host "Found $($mapFiles.Count) map file(s) to upload" -ForegroundColor Cyan

    # connect
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
        $session.Open($sessionOptions)
        Write-Host "Connected" -ForegroundColor Green

        # create remote maps directory if it doesn't exist
        if (-not $session.FileExists($RemoteMapsPath)) {
            Write-Host "Creating remote maps directory..." -ForegroundColor Cyan
            $session.CreateDirectory($RemoteMapsPath)
        }

        # upload each map file
        foreach ($file in $mapFiles) {
            Write-Host "Uploading $($file.Name)..." -ForegroundColor Cyan
            $transferResult = $session.PutFiles(
                $file.FullName,
                "$RemoteMapsPath/$($file.Name)"
            )
            $transferResult.Check()
            Write-Host "Uploaded $($file.Name)" -ForegroundColor Green
        }

        Write-Host "All maps uploaded successfully" -ForegroundColor Green
    }
    finally {
        $session.Dispose()
    }
}
catch {
    Write-Host "Something went wrong: $_" -ForegroundColor Red
}
finally {
    Set-Location $PSScriptRoot
}