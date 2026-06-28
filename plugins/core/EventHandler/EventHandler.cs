using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class EventHandler(
    RetakeExecutes _plugin,
    AfkService _afkService,
    GameService _gameService,
    UtilService _utilService,
    QueueService _queueService,
    WeaponAllocationService _weaponAllocationService,
    RoundAllocationService _roundAllocationService,
    PlayerDataService _playerDataService,
    SkinChangerService _skinChangerService,
    KickBanService _kickBanService,
    MapVotingService _mapVotingService,
    GameState _gameState
)
{
    private readonly RetakeExecutes plugin = _plugin;
    private readonly AfkService afkService = _afkService;
    private readonly GameState gameState = _gameState;
    private readonly GameService gameService = _gameService;
    private readonly UtilService utilService = _utilService;
    private readonly QueueService queueService = _queueService;
    private readonly WeaponAllocationService weaponAllocationService = _weaponAllocationService;
    private readonly RoundAllocationService roundAllocationService = _roundAllocationService;
    private readonly PlayerDataService playerDataService = _playerDataService;
    private readonly SkinChangerService skinChangerService = _skinChangerService;
    private readonly KickBanService kickBanService = _kickBanService;

    private readonly MapVotingService mapVotingService = _mapVotingService;
    private readonly HashSet<ulong> processedConnections = [];

    private float _bombPlantedTime;
    private bool _bombTicking;

    public void RegisterEvents()
    {
        plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        plugin.RegisterEventHandler<EventRoundPrestart>(OnPreRoundStart);
        plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        plugin.RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
        plugin.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        plugin.RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        plugin.RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
        plugin.RegisterEventHandler<EventBombBegindefuse>(OnBombDefuse);
    }

    public void DeregisterEvents()
    {
        plugin.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        plugin.DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        plugin.DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        plugin.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        plugin.DeregisterEventHandler<EventRoundPrestart>(OnPreRoundStart);
        plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
        plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        plugin.DeregisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
        plugin.DeregisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        plugin.DeregisterEventHandler<EventPlayerChat>(OnPlayerChat);
        plugin.DeregisterEventHandler<EventBombPlanted>(OnBombPlanted);
    }

    #region OnPreRoundStart
    private HookResult OnPreRoundStart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (GameHelpers.IsWarmup())
        {
            return HookResult.Continue;
        }

        if (!string.IsNullOrEmpty(gameState.RtvMidGameNextMap))
        {
            Log.Info($"Changing map to RTV'd map {gameState.RtvMidGameNextMap}");
            Server.ExecuteCommand($"map {gameState.RtvMidGameNextMap}");
            gameState.RtvMidGameNextMap = string.Empty;
            gameState.EndGameNextMap = string.Empty;
        }

        // gameService.ScrambleTeams();
        gameService.SwapTeams();

        gameService.AutobalanceTeams();
        gameService.AssignSpectatorsToTeam();
        gameService.PickBombCarrier();
        gameService.SetRoundStrategy();

        roundAllocationService.SetRoundType();

        if (gameState.CurrentRoundStrategy is not null)
        {
            // gameState.IsRoundLive = true;
        }

        if (gameState.CurrentRoundType == RoundType.FullBuy)
        {
            gameService.AssignAwpToPlayers();
        }

        plugin.AddTimer(0.05f, gameService.AssignPlayerSpawns);

        return HookResult.Continue;
    }
    #endregion

    #region OnPlayerConnectFull
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        Log.Info("On Player Connect loaded");

        if (@event is null)
        {
            Log.Warn("Event player connect full that fired was null");
            return HookResult.Continue;
        }

        var player = @event.Userid;

        if (player is null)
            return HookResult.Continue;

        afkService.TrackPlayer(player.SteamID);

        var steamId = player.SteamID;
        var steamName = player.PlayerName;

        // This stops duplicate pre round events from being called even when there are no players.
        if (!processedConnections.Add(player.SteamID))
        {
            Log.Info("Duplicate processed connections called. Blocking further.");
            return HookResult.Continue;
        }

        kickBanService.CheckPlayerBans(player);

        Task.Run(async () =>
        {
            try
            {
                await playerDataService.RetrievePlayerData(player.SteamID);
                await playerDataService.RetrievePlayerSkins(player.SteamID);
                await playerDataService.RecordPlayerEvent(
                    player.SteamID,
                    PlayerEventType.ServerConnect
                );
                Server.NextFrame(() =>
                {
                    skinChangerService.ApplyKnifeAndGloves(player.SteamID);
                    skinChangerService.ApplyPlayerSkins(steamId, RoundType.None);
                });

                playerDataService.PrintPlayerData();
            }
            catch (Exception ex)
            {
                DbExceptionHandler.Handle(ex);
            }
        });

        var totalPlayers = GameHelpers.GetValidPlayers();

        plugin.AddTimer(
            3f,
            () =>
            {
                player.PrintToChat(
                    $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} ✪ MACROS RETAKE EXECUTES ✪ "
                );
                player.PrintToChat(
                    $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Welcome {player.PlayerName}! "
                );

                player.PrintToChat(new string('-', 86));

                player.PrintToChat(
                    $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} This game mode is in beta. If you notice a bug, type {ChatColors.Green} !help {ChatColors.White} or {ChatColors.Green} !report {ChatColors.White} to request help or report a problem."
                );

                player.PrintToChat(
                    $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Visit {ChatColors.Green} www.macroscs2.com {ChatColors.White} to change your weapon preferences and skins!"
                );

                player.PrintToChat(
                    $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Make sure to type {ChatColors.Red} !reload {ChatColors.White} in chat to update your game."
                );
                player.PrintToChat(new string('-', 86));
            }
        );

        // The streamline start script for some reason spawns bots on a fresh server.
        // This ensures the round goes into warmup mode if a player joins.
        Log.Info($"PRE USER JOIN ROUND PLAYED??? {gameState.RoundsPlayed}");
        if (gameState.IsRoundLive && gameState.RoundsPlayed != 0)
        {
            player.PrintToChat(
                $" {ChatColors.Green} [MACROS]: >>> Round is live {ChatColors.White} You have been added to the queue."
            );

            queueService.AddSpectator(steamId, steamName);

            player.ChangeTeam(CsTeam.Spectator);

            return HookResult.Continue;
        }

        if (totalPlayers.Count == 1)
        {
            Server.ExecuteCommand("bot_kick");

            Log.Info($"Player {steamName} is first on the server.");

            queueService.AddActivePlayer(steamId, steamName, true, out var team);

            player.SwitchTeam(team);

            plugin.AddTimer(2f, player.Respawn);

            plugin.AddTimer(
                5.0f,
                () =>
                {
                    player.PrintToChat(
                        $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Retake Executes requires {ChatColors.Green} 2 players {ChatColors.White} to play... waiting for second player."
                    );
                }
            );

            SetWarmupPendingPlayers();

            return HookResult.Continue;
        }

        if (totalPlayers.Count >= Constants.MIN_PLAYERS_TO_START)
        {
            Log.Info($"Player {steamName} has joined.");

            queueService.AddActivePlayer(steamId, steamName, false, out var team);

            player.SwitchTeam(team);

            ServerHelper.ExecuteRetakeExecuteMode();

            plugin.AddTimer(1.0f, () => gameState.HasMatchStarted = true);
        }

        return HookResult.Continue;
    }
    #endregion

    #region  OnPlayerDisconnect
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var steamId = @event.Userid!.SteamID;

        processedConnections.Remove(steamId);
        queueService.Remove(steamId);
        playerDataService.RemovePlayerData(steamId);
        afkService.RemovePlayer(steamId);

        mapVotingService.RemoveFromVoteList(steamId);
        // mapVotingService.CheckVoteResult();

        Task.Run(async () =>
        {
            try
            {
                await playerDataService.RecordPlayerEvent(
                    steamId,
                    PlayerEventType.ServerDisconnect
                );
            }
            catch (Exception ex)
            {
                DbExceptionHandler.Handle(ex);
            }
        });

        var totalPlayers = GameHelpers.GetValidPlayers();

        if (totalPlayers.Count == 1)
        {
            SetWarmupPendingPlayers();
            gameState.RoundsPlayed = 0;
        }

        Log.Info($"Player disconnect called. Players: {totalPlayers.Count}");
        return HookResult.Continue;
    }
    #endregion

    #region OnRoundStart
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _bombPlantedTime = float.NaN;
        _bombTicking = false;

        var gameRules = GameHelpers.GetGameRules();

        if (gameRules is not null && gameRules.WarmupPeriod)
        {
            return HookResult.Continue;
        }

        var currentRoundStrategy = gameState.GetCurrentRoundStrategy();
        var currentRoundType = gameState.GetCurrentRoundTypeForAnnouncement();

        if (currentRoundStrategy is null)
        {
            return HookResult.Continue;
        }

        var totalPlayers = GameHelpers.GetValidPlayers();

        if (totalPlayers.Count == 0)
        {
            Log.Info("No valid players found. Not processing.");
            return HookResult.Continue;
        }

        var roundType = gameState.GetCurrentRoundType();
        gameState.IsRoundLive = true;

        utilService.ThrowRoundUtility();

        Server.PrintToChatAll(new string('-', 65));
        Server.PrintToChatAll(
            $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} {currentRoundStrategy.RoundStrategy} | {currentRoundType}"
        );
        Server.PrintToChatAll(new string('-', 65));

        foreach (var player in totalPlayers)
        {
            if (player.Team == CsTeam.Spectator)
                continue;

            player.PrintToCenterAlert(currentRoundStrategy.RoundStrategy);
            var playerData = playerDataService.playerData[player.SteamID];

            weaponAllocationService.AssignPlayerWeaponsAndUtility(player, roundType, playerData);
        }

        plugin.AddTimer(
            0.01f,
            () =>
            {
                foreach (var player in totalPlayers)
                {
                    skinChangerService.ApplyKnifeAndGloves(player.SteamID);
                    skinChangerService.ApplyPlayerSkins(player.SteamID, roundType);
                }
            }
        );

        if (gameState.RoundsPlayed == 10)
        {
            Server.PrintToChatAll(
                $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Want to change the map? Type {ChatColors.Green} !rtv {ChatColors.White} and vote!"
            );
        }

        if (gameState.RoundsPlayed == 15)
        {
            mapVotingService.StartMapVote(true);
        }

        return HookResult.Continue;
    }
    #endregion

    #region OnRoundEnd
    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        gameService.TrackRoundEnd(@event.Winner);

        var roundWinner = GameHelpers.GetRoundWinnerString(@event.Winner);
        var mapName = Server.MapName;
        var roundStrategy = gameState.GetCurrentRoundStrategy().RoundStrategy;
        var roundType = gameState.GetCurrentRoundTypeForAnnouncement();
        var bombPlanted = _bombTicking;

        var remainingT = Utilities
            .GetPlayers()
            .Where(player => player.PawnIsAlive)
            .Count(player => player.Team == CsTeam.Terrorist);

        var remainingCT = Utilities
            .GetPlayers()
            .Where(player => player.PawnIsAlive)
            .Count(player => player.Team == CsTeam.CounterTerrorist);

        var totalPlayerCount = queueService.GetActivePlayerCount();

        var roundEventLog = new RoundEventLog
        {
            BombPlanted = bombPlanted,
            MapName = mapName,
            RemainingCt = remainingCT,
            RemainingT = remainingT,
            RoundType = roundType ?? "NA",
            StrategyType = roundStrategy,
            WinningSide = roundWinner,
            TotalPlayerCount = totalPlayerCount,
        };

        Task.Run(async () =>
        {
            await playerDataService.RecordEventLog(roundEventLog);
        });

        return HookResult.Continue;
    }
    #endregion


    #region OnPlayerSpawn
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        Log.Info("On Player Spawn called");
        var player = @event.Userid;
        if (
            player == null
            || !player.IsValid
            || player.Team == CsTeam.Spectator
            || player.Team == CsTeam.None
        )
            return HookResult.Continue;

        return HookResult.Continue;
    }
    #endregion

    #region OnMatchEnd
    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        gameService.SelectAndLoadNextMap();
        mapVotingService.ClearVoteList();
        return HookResult.Continue;
    }
    #endregion

    #region On Grenade Thrown
    private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
    {
        // Don't want to be logging the flashbangs to console if we are 'live'.
        if (!ServerHelper.IsPracMode)
        {
            return HookResult.Continue;
        }

        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // var projectile = Utilities
            //     .FindAllEntitiesByDesignerName<CBaseCSGrenadeProjectile>("flashbang_projectile")
            //     .MinBy(p =>
            //         GameHelpers.DistanceTo(p.AbsOrigin!, player.PlayerPawn.Value!.AbsOrigin!)
            //     );
            // if (projectile == null)
            //     return;

            var smokeProjectile = Utilities
                .FindAllEntitiesByDesignerName<CBaseCSGrenadeProjectile>("smokegrenade_projectile")
                .MinBy(p =>
                    GameHelpers.DistanceTo(p.AbsOrigin!, player.PlayerPawn.Value!.AbsOrigin!)
                );

            if (smokeProjectile == null)
                return;

            Log.Info($"Position: {smokeProjectile.AbsOrigin}");
            Log.Info($"Angle: {smokeProjectile.AbsRotation}");
            Log.Info($"Velocity: {smokeProjectile.AbsVelocity}");

            Server.PrintToConsole($"Position: {smokeProjectile.AbsOrigin}");
            Server.PrintToConsole($"Angle: {smokeProjectile.AbsRotation}");
            Server.PrintToConsole($"Velocity: {smokeProjectile.AbsVelocity}");

            Server.PrintToChatAll($"Position: {smokeProjectile.AbsOrigin}");
            Server.PrintToChatAll($"Angle: {smokeProjectile.AbsRotation}");
            Server.PrintToChatAll($"Velocity: {smokeProjectile.AbsVelocity}");

            Log.Info(
                $"{{\n"
                    + $"  \"name\": \"\",\n"
                    + $"  \"origin\": \"{smokeProjectile.AbsOrigin!.X:F2}, {smokeProjectile.AbsOrigin!.Y:F2}, {smokeProjectile.AbsOrigin!.Z:F2}\",\n"
                    + $"  \"velocity\": \"{smokeProjectile.AbsVelocity!.X:F2}, {smokeProjectile.AbsVelocity!.Y:F2}, {smokeProjectile.AbsVelocity!.Z:F2}\",\n"
                    + $"  \"angle\": \"{smokeProjectile.AbsRotation!.X:F2}, {smokeProjectile.AbsRotation!.Y:F2}, {smokeProjectile.AbsRotation!.Z:F2}\"\n"
                    + $"}}"
            );
        });

        return HookResult.Continue;
    }
    #endregion

    #region OnPlayerChat
    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        string[] ChatMessagesRelatedToPreferences =
        [
            "!guns",
            "!ak",
            "!awp",
            "!m4",
            "!deagle",
            "!glock",
            "!usp",
            "!pistol",
            "!rifle",
            "!weapon",
            "!gun",
            "!knife",
            "!m9",
            "!ws",
            "!skins",
        ];

        if (ChatMessagesRelatedToPreferences.Contains(@event.Text.Trim()))
        {
            var userId = @event.Userid;
            var player = Utilities.GetPlayerFromUserid(userId);

            if (player is null)
            {
                return HookResult.Continue;
            }

            player.PrintToChat(
                $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Visit {ChatColors.Green} www.macroscs2.com {ChatColors.White} to update your weapon preferences and skins!"
            );
            player.PrintToChat(
                $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Make sure to type {ChatColors.Red} !reload {ChatColors.White} in chat to update your game."
            );
        }

        return HookResult.Continue;
    }
    #endregion

    #region Set Warmup
    private static void SetWarmupPendingPlayers()
    {
        Server.ExecuteCommand("mp_warmup_start");
        Server.ExecuteCommand("mp_warmup_pausetimer 1");
        Log.Info("Server is in warmup mode until a player joins...");
    }
    #endregion


    #region On Bomb Planted
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        Log.Info($"PLANTED ON SITE {@event.Site}");
        var site = @event.Site == 0 ? "A" : "B";
        var player = @event.Userid.PlayerName;

        Server.PrintToChatAll(
            $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} {player} has planted the bomb on {site}!"
        );

        _bombPlantedTime = Server.CurrentTime;
        _bombTicking = true;

        return HookResult.Continue;
    }
    #endregion

    #region On Bomb Defuse
    public HookResult OnBombDefuse(EventBombBegindefuse @event, GameEventInfo info)
    {
        var bomb = Utilities
            .FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4")
            .FirstOrDefault();

        var isAllTerroristDead = Utilities
            .GetPlayers()
            .Where(player => player.Team == CsTeam.Terrorist)
            .Where(player => player.Pawn.IsValid)
            .All(player => !player.PawnIsAlive);

        if (bomb is null || !bomb.IsValid || bomb.CannotBeDefused)
        {
            Log.Info("Bomb is not in valid state to test defusal!");
            return HookResult.Continue;
        }

        if (!isAllTerroristDead)
        {
            Log.Info("Terrorists are not dead!");
            return HookResult.Continue;
        }

        var timeUntilDetonation = bomb.TimerLength - (Server.CurrentTime - _bombPlantedTime);

        if (timeUntilDetonation < 5.0f)
        {
            Server.NextFrame(() =>
            {
                bomb.C4Blow = 0.0f;
            });
        }
        else
        {
            Server.NextFrame(() =>
            {
                bomb.DefuseCountDown = 0.0f;
            });
        }

        return HookResult.Continue;
    }
    #endregion
}
