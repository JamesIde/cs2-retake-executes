using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace RetakeExecutesPlugin;

public class RetakeExecutes : BasePlugin, IPluginConfig<PluginConfiguration>
{
    public override string ModuleName => "Retake Executes";
    public override string ModuleVersion => "0.0.1";

    public PluginConfiguration? Config { get; set; }

    // Required to read the RetakeExecutes.jsonc config file
    public void OnConfigParsed(PluginConfiguration config)
    {
        Config = config;
    }

    private EventHandler? _eventManager;
    private StaticCommandManager? _staticCommandManager;
    private CtorCommandManager? _ctorCommandManager;
    private GameService? _gameService;
    private AfkService? _afkService;
    private MapVotingService? _votingService;

    public override void Load(bool hotReload)
    {
        Log.Initialize(Logger);

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        _afkService = new();

        _afkService.Clear();

        // AddTimer(30f, _afkService.CheckPlayerStatus, TimerFlags.REPEAT);

        _staticCommandManager = new StaticCommandManager(this);
        _staticCommandManager.RegisterCommands();
        Log.Info("Loaded static commands");
    }

    private void OnMapStart(string mapName)
    {
        Log.Info($"Loaded {mapName} map");
        ServerHelper.SetPracMode(false);

        MapConfiguration.Empty();

        var mapService = new MapService();
        var mapConfig =
            mapService.InitialiseMapConfiguration(ModuleDirectory, mapName)
            ?? throw new Exception($"Failed to load map configuration for {mapName}");

        var gameState = new GameState();
        gameState.ResetStreakThreshold();
        gameState.ResetGameState();

        var teleportService = new TeleportService();
        var queueService = new QueueService();
        var spawnService = new SpawnService();
        var utilService = new UtilService(this, gameState, mapConfig);
        var weaponAllocationService = new WeaponAllocationService(gameState);
        var roundAllocationService = new RoundAllocationService(gameState);

        var databaseService = new DatabaseService(
            Config!.DatabaseConnectionString!,
            Config.DatabaseSchema!
        );

        var playerDataService = new PlayerDataService(databaseService);

        var skinChangerService = new SkinChangerService(this, playerDataService, gameState);
        var kickBanService = new KickBanService(this, playerDataService);
        _votingService = new MapVotingService(this, gameState);

        _gameService = new(
            this,
            gameState,
            mapConfig,
            teleportService,
            queueService,
            spawnService,
            playerDataService
        );

        _eventManager = new EventHandler(
            this,
            _afkService,
            _gameService,
            utilService,
            queueService,
            weaponAllocationService,
            roundAllocationService,
            playerDataService,
            skinChangerService,
            kickBanService,
            _votingService,
            gameState
        );

        _eventManager.RegisterEvents();
        Log.Info("Loaded events");

        _ctorCommandManager = new(
            this,
            utilService,
            _gameService,
            playerDataService,
            skinChangerService,
            _votingService
        );

        _ctorCommandManager.RegisterCommands();

        Log.Info("Loaded ctor commands");

        SetWarmupPendingPlayers();
    }

    private static void SetWarmupPendingPlayers()
    {
        Server.ExecuteCommand("mp_warmup_start");
        Server.ExecuteCommand("mp_warmup_pausetimer 1");
        Log.Info("Server is in warmup mode until a player joins...");
    }

    public void OnMapEnd()
    {
        _ctorCommandManager?.DeregisterCommands();
        _eventManager?.DeregisterEvents();

        _eventManager = null;
        _gameService = null;

        _staticCommandManager = null;
        _ctorCommandManager = null;

        ServerHelper.SetPracMode(false);
    }
}
