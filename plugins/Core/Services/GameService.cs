using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class GameService(
    BasePlugin _plugin,
    GameState _gameState,
    MapConfiguration _mapConfiguration,
    TeleportService _teleportService,
    QueueService _queueService,
    SpawnService _spawnService,
    PlayerDataService _playerDataService
)
{
    private readonly BasePlugin plugin = _plugin;
    private readonly GameState gameState = _gameState;
    private readonly MapConfiguration mapConfiguration = _mapConfiguration;
    private readonly TeleportService teleportService = _teleportService;
    private readonly QueueService queueService = _queueService;
    private readonly SpawnService spawnService = _spawnService;
    private readonly PlayerDataService playerDataService = _playerDataService;
    private static readonly Random random = new();

    #region Set Round Strategies
    public void SetRoundStrategy()
    {
        var numberOfRoundPermutations = mapConfiguration.GetRoundCount();

        var selectedRoundId = random.Next(1, numberOfRoundPermutations + 1);

        var selectedRound = mapConfiguration.GetRoundById(selectedRoundId);

        gameState.SetCurrentRoundStrategy(selectedRound);
    }
    #endregion

    #region AssignPlayerSpawns
    public void AssignPlayerSpawns()
    {
        var selectedRound =
            gameState.GetCurrentRoundStrategy()
            ?? throw new Exception("Selected round is null. Assign player spawn cannot proceed.");

        Log.Info($"Assigning spawns for {selectedRound.RoundStrategy}");

        queueService.PrintQueue();

        var serverPlayersBySteamId = GameHelpers.GetValidPlayers().ToDictionary(p => p.SteamID);

        var activePlayers = queueService.GetPlayerQueue();

        var terrorists = activePlayers
            .Where(p => p.Value.Team == CsTeam.Terrorist)
            .Select(p => p.Key)
            .ToList();

        var counterTerrorists = activePlayers
            .Where(p => p.Value.Team == CsTeam.CounterTerrorist)
            .Select(p => p.Key)
            .ToList();

        var tAssignedPlayers = spawnService.AllocateSpawnToPlayers(
            terrorists,
            selectedRound.Spawns.TerroristSpawns
        );

        var ctAssignedPlayers = spawnService.AllocateSpawnToPlayers(
            counterTerrorists,
            selectedRound.Spawns.CounterTerroristSpawns
        );

        if (serverPlayersBySteamId.Count == 0)
            return;

        var allPlayers = tAssignedPlayers
            .Concat(ctAssignedPlayers)
            .ToDictionary(x => x.Key, x => x.Value);

        foreach (var (key, value) in allPlayers)
        {
            if (!serverPlayersBySteamId.TryGetValue(key, out var playerPawn))
                continue;

            var playerSpawn = allPlayers[key];

            if (playerPawn.Team != playerSpawn.Team)
            {
                playerPawn.ChangeTeam(playerSpawn.Team);
                Log.Info(
                    $"Player {playerPawn.PlayerName} needs to be changed from {playerPawn.Team} to {playerSpawn.Team}"
                );
            }
            teleportService.TeleportPlayer(playerPawn, playerSpawn);
        }
    }
    #endregion

    #region Assign Awp
    public void AssignAwpToPlayers()
    {
        var totalPlayers = queueService.GetActivePlayerCount();

        if (totalPlayers < Constants.MIN_PLAYERS_TO_ASSIGN_AWP)
            return;

        var ctPlayerIds = Utilities
            .GetPlayers()
            .Where(p => p.Team == CsTeam.CounterTerrorist)
            .Select(p => p.SteamID);

        var tPlayerIds = Utilities
            .GetPlayers()
            .Where(p => p.Team == CsTeam.Terrorist)
            .Select(p => p.SteamID);

        var ctSideUsersWantingAwp = playerDataService.RetrievePlayersWantingAwp(
            CsTeam.CounterTerrorist,
            ctPlayerIds
        );
        var tSideUsersWantingAwp = playerDataService.RetrievePlayersWantingAwp(
            CsTeam.Terrorist,
            tPlayerIds
        );

        // 50% chance of assigning an awp to one player
        var shouldAssign = WeaponHelper.ShouldRoll(Constants.PERCENTAGE_CHANCE_ROLL_AWP);

        if (shouldAssign)
        {
            if (tSideUsersWantingAwp.Count > 0)
            {
                var selectedUser = GameHelpers.PickRandom(tSideUsersWantingAwp);
                Log.Info($"{selectedUser} assigned awp");
                gameState.SelectedTAwper = selectedUser;
            }

            if (ctSideUsersWantingAwp.Count > 0)
            {
                var selectedUser = GameHelpers.PickRandom(ctSideUsersWantingAwp);
                Log.Info($"{selectedUser} assigned awp");
                gameState.SelectedCTAwper = selectedUser;
            }
        }
    }
    #endregion

    #region Select Next Map
    public void SelectAndLoadNextMap()
    {
        string nextMap = !string.IsNullOrEmpty(gameState.EndGameNextMap)
            ? gameState.EndGameNextMap
            : "de_mirage";

        if (string.IsNullOrEmpty(gameState.EndGameNextMap))
        {
            Server.PrintToChatAll(
                $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} No map was voted on. Defaulting to {nextMap}"
            );
        }

        Server.PrintToChatAll(
            $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Loading {nextMap}"
        );

        plugin.AddTimer(
            6f,
            () =>
            {
                Server.NextFrame(() =>
                {
                    Server.ExecuteCommand($"map {nextMap}");
                });
            }
        );

        gameState.ResetGameState();
        gameState.ResetStreakThreshold();

        var currentMap = Server.MapName;

        List<string> maps =
        [
            "de_dust2",
            "de_mirage",
            "de_cache",
            "de_inferno",
            "de_nuke",
            "de_anubis",
            "de_overpass",
        ];
    }
    #endregion

    #region Assign Spectators
    public void AssignSpectatorsToTeam()
    {
        var serverPlayersBySteamId = GameHelpers.GetValidPlayers().ToDictionary(p => p.SteamID);

        var playersSpectator = queueService
            .GetPlayerQueue()
            .Where(p => p.Value.Status == PlayerStatus.Queued)
            .ToDictionary();

        if (serverPlayersBySteamId.Count == 0 || playersSpectator.Count == 0)
        {
            return;
        }

        var activePlayerCount = queueService.GetActivePlayerCount();

        if (activePlayerCount == 10 && playersSpectator.Count > 0)
        {
            Log.Info($"Found {playersSpectator.Count} queued players.");

            foreach (var (player, value) in playersSpectator)
            {
                serverPlayersBySteamId[player]
                    .PrintToChat(
                        "[MACROS]: >>> You are in queue.. Please wait until there is room."
                    );
            }
            return;
        }

        foreach (var (player, value) in playersSpectator)
        {
            queueService.AssignSpectatorToTeam(player, out var team);

            serverPlayersBySteamId[player].ChangeTeam(team);

            Log.Info($"Spectator {player} has been assigned {team}");
        }
    }
    #endregion

    #region Autobalance Teams
    public void AutobalanceTeams()
    {
        var serverPlayersBySteamId = GameHelpers.GetValidPlayers().ToDictionary(p => p.SteamID);

        if (serverPlayersBySteamId.Count == 0)
            return;

        var (fromCt, toMove) = queueService.RebalanceTeams();

        if (toMove == 0)
        {
            Log.Info($"No players to move. Teams are even.");
            return;
        }

        var side = fromCt ? "from CT to T" : "from T to CT";
        Log.Info($"Found {toMove} player(s) to move {side}. Starting process.");

        var activePlayers = queueService.GetPlayerQueue();

        SwitchPlayers(fromCt ? CsTeam.CounterTerrorist : CsTeam.Terrorist);

        void SwitchPlayers(CsTeam fromTeam)
        {
            var toTeam =
                fromTeam == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist;

            var idsToSwitch = activePlayers
                .Where(p => p.Value.Team == fromTeam)
                .OrderByDescending(p => p.Value.DateConnected)
                .Take(toMove)
                .Select(p => p.Key)
                .ToList();

            foreach (var steamId in idsToSwitch)
            {
                if (!serverPlayersBySteamId.TryGetValue(steamId, out var player) || !player.IsValid)
                    continue;

                Log.Info($"Moved {player.SteamID} from {fromTeam} to {toTeam}");

                player.ChangeTeam(toTeam);

                player.PrintToChat(
                    $" {ChatColors.Green}[MACROS]: >>> {ChatColors.White} You have been switched teams due to autobalance."
                );
                queueService.UpdatePlayerTeam(steamId, toTeam);
            }
        }
    }
    #endregion

    #region Swap Teams
    public void SwapTeams()
    {
        var currentRoundsPlayed = gameState.RoundsPlayed;

        if (currentRoundsPlayed == 0 || currentRoundsPlayed == 1)
            return;

        if (currentRoundsPlayed % 4 != 0)
            return;

        Log.Info("Swapping teams");

        var serverPlayersBySteamId = GameHelpers.GetValidPlayers().ToDictionary(p => p.SteamID);
        var ctPlayers = queueService.GetActivePlayers(CsTeam.CounterTerrorist);
        var tPlayers = queueService.GetActivePlayers(CsTeam.Terrorist);

        foreach (var player in ctPlayers)
        {
            if (!serverPlayersBySteamId.TryGetValue(player, out var controller))
                continue;

            controller.SwitchTeam(CsTeam.Terrorist);
            queueService.UpdatePlayerTeam(controller.SteamID, CsTeam.Terrorist);
        }

        foreach (var player in tPlayers)
        {
            if (!serverPlayersBySteamId.TryGetValue(player, out var controller))
                continue;

            controller.SwitchTeam(CsTeam.CounterTerrorist);
            queueService.UpdatePlayerTeam(controller.SteamID, CsTeam.CounterTerrorist);
        }
    }
    #endregion

    #region Track Round End
    public void TrackRoundEnd(int winner)
    {
        gameState.IsRoundLive = false;
        gameState.RoundsPlayed++;

        var roundWinner = GameHelpers.GetRoundWinner(winner);

        Log.Info($"Winner is {roundWinner}");

        if (roundWinner == CsTeam.Terrorist)
        {
            gameState.TConsecutiveWins++;
            gameState.LastRoundWin = CsTeam.Terrorist;
            gameState.CTConsecutiveWins = 0;
            Log.Info($"T Side Streak is {gameState.TConsecutiveWins} ");
        }

        if (roundWinner == CsTeam.CounterTerrorist)
        {
            gameState.CTConsecutiveWins++;
            gameState.LastRoundWin = CsTeam.CounterTerrorist;
            gameState.TConsecutiveWins = 0;
            Log.Info($"CT Side Streak is {gameState.CTConsecutiveWins} ");
        }

        // CheckStreakThreshold();
    }
    #endregion

    #region Check Streak Threshold
    private void CheckStreakThreshold()
    {
        var tWins = gameState.TConsecutiveWins;
        var ctWins = gameState.CTConsecutiveWins;
        var maxWins = Constants.MAX_CONSECUTIVE_ROUNDS;

        if (
            tWins == Constants.FOUR_CONSECUTIVE_ROUNDS
            || tWins == Constants.FIVE_CONSECUTIVE_ROUNDS
        )
        {
            Server.PrintToChatAll(
                $" {ChatColors.Silver} [MACROS]: >>> Ts are on a {tWins} win streak! Teams will be scrambled at {maxWins} wins!"
            );
        }

        if (
            ctWins == Constants.FOUR_CONSECUTIVE_ROUNDS
            || ctWins == Constants.FIVE_CONSECUTIVE_ROUNDS
        )
        {
            Server.PrintToChatAll(
                $" {ChatColors.Silver} [MACROS]: >>> CTs are on a {ctWins} win streak! Teams will be scrambled at {maxWins} wins!"
            );
        }

        if (tWins == maxWins || ctWins == maxWins)
        {
            Server.PrintToChatAll(
                $" {ChatColors.Gold} [MACROS]: >>> Teams will be scrambled next round!"
            );
            gameState.CanScrambleTeams = true;
            gameState.ResetStreakThreshold();
        }
    }
    #endregion

    #region Scramble Teams
    public void ScrambleTeams()
    {
        if (!gameState.CanScrambleTeams)
            return;
        var scrambledPlayers = queueService
            .GetPlayerQueue()
            .Keys.OrderBy(_ => random.Next())
            .ToList();

        Log.Info($"Found {scrambledPlayers.Count} to scramble");

        var serverPlayersBySteamId = GameHelpers.GetValidPlayers().ToDictionary(p => p.SteamID);

        if (serverPlayersBySteamId.Count == 0 || scrambledPlayers.Count == 0)
        {
            return;
        }

        foreach (var (player, index) in scrambledPlayers.Select((value, i) => (value, i)))
        {
            var team = index % 2 == 0 ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

            queueService.UpdatePlayerTeam(player, team);

            // No need to change the player team to the one they may already be on.
            if (serverPlayersBySteamId[player].Team != team)
            {
                if (!serverPlayersBySteamId[player].Pawn.IsValid)
                {
                    Log.Info($"Team Scrambling Error: Pawn is not valid. {player}");
                    continue;
                }
                serverPlayersBySteamId[player].SwitchTeam(team);
            }
        }

        gameState.CanScrambleTeams = false;
    }
    #endregion

    #region Set Bomb Carrier
    public void PickBombCarrier()
    {
        var carrier = queueService
            .GetPlayerQueue()
            .Where(kvp => kvp.Value.Team == CsTeam.Terrorist)
            .Select(v => v.Key)
            .OrderBy(_ => random.Next())
            .FirstOrDefault();

        if (carrier == 0)
        {
            Log.Info("No terrorists to assign bomb.");
            return;
        }
        gameState.SelectedBombCarrier = carrier;
    }
    #endregion
}
