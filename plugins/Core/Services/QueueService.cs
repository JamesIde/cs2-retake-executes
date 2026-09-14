using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class QueueService(int maxDifference = 1)
{
    private static readonly Random random = new();
    private Dictionary<ulong, PlayerState> playerQueue = [];

    #region Add / Remove From Queue
    public void Remove(ulong steamId) => playerQueue.Remove(steamId);

    public Dictionary<ulong, PlayerState> GetPlayerQueue()
    {
        return playerQueue;
    }
    #endregion

    #region Get Active Players
    public int GetActivePlayerCount()
    {
        return playerQueue.Values.Count(player => player.Status == PlayerStatus.Active);
    }

    public List<ulong> GetActivePlayers(CsTeam team)
    {
        return
        [
            .. playerQueue
                .Where(player =>
                    player.Value.Team == team && player.Value.Status == PlayerStatus.Active
                )
                .Select(player => player.Key),
        ];
    }
    #endregion


    #region Add Players to Team
    public void AddActivePlayer(
        ulong steamId,
        string steamName,
        bool isFirstPlayer,
        out CsTeam team
    )
    {
        team = DetermineTeam();
        if (isFirstPlayer)
        {
            Log.Info($"{steamId}, {steamName}, {team} is first player on the server.");

            playerQueue.Add(
                steamId,
                new PlayerState
                {
                    PlayerName = steamName,
                    DateConnected = DateTime.UtcNow,
                    Status = PlayerStatus.Active,
                    Team = team,
                }
            );
            return;
        }

        Log.Info($"{steamId}, {steamName} added to queue.");

        playerQueue.Add(
            steamId,
            new PlayerState
            {
                PlayerName = steamName,
                DateConnected = DateTime.UtcNow,
                Status = PlayerStatus.Active,
                Team = team,
            }
        );
    }

    public void AssignSpectatorToTeam(ulong steamId, out CsTeam team)
    {
        team = DetermineTeam();
        playerQueue[steamId].Team = team;
        playerQueue[steamId].Status = PlayerStatus.Active;
    }

    public void UpdatePlayerTeam(ulong steamId, CsTeam team)
    {
        playerQueue[steamId].Team = team;
    }

    public void AddSpectator(ulong steamId, string steamName)
    {
        Log.Info($"{steamId}, {steamName} added to spectator.");

        playerQueue.Add(
            steamId,
            new PlayerState
            {
                PlayerName = steamName,
                DateConnected = DateTime.UtcNow,
                Status = PlayerStatus.Queued,
                Team = CsTeam.Spectator,
            }
        );
    }
    #endregion

    #region Determine Team Logic
    private CsTeam DetermineTeam()
    {
        int numTerrorists = CountOf(CsTeam.Terrorist);
        int numCTs = CountOf(CsTeam.CounterTerrorist);

        if (numTerrorists == 0 && numCTs == 0)
            return RandomTeam();

        // 1v1 - send next guy to T
        if (numTerrorists == 1 && numCTs == 1)
            return CsTeam.Terrorist;

        if (numTerrorists - numCTs >= maxDifference)
            return CsTeam.CounterTerrorist;

        if (numCTs - numTerrorists >= maxDifference)
            return CsTeam.Terrorist;

        return RandomTeam();
    }

    private int CountOf(CsTeam team) => playerQueue.Values.Count(p => p.Team == team);

    private static CsTeam RandomTeam() =>
        random.Next(2) == 0 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;

    #endregion

    #region Rebalance Teams
    public (bool fromCt, int toMove) RebalanceTeams()
    {
        const int maxDiff = 2;
        int numTerrorists = CountOf(CsTeam.Terrorist);
        int numCTs = CountOf(CsTeam.CounterTerrorist);

        int diff = Math.Abs(numCTs - numTerrorists);
        if (diff < maxDiff)
            return (false, 0);

        // 5v1 → move 2 → 3v3;  4v2 → move 1 → 3v3
        int toMove = diff / 2;
        if (numCTs > numTerrorists)
            return (true, toMove);
        else
            return (false, toMove);
    }
    #endregion

    /// <summary>
    /// Mainly for debugging purposes
    /// </summary>
    public void PrintQueue()
    {
        Log.Info("--------------------- Start Player Queue ---------------------");
        foreach (var (key, value) in playerQueue)
        {
            Log.Info($"{key}: {value.PlayerName}, {value.Status}, {value.Team}");
        }
        Log.Info("--------------------- End Player Queue ---------------------");
    }
}
