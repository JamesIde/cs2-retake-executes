using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class AfkService
{
    public Dictionary<ulong, int> AfkTracker = [];
    private static readonly int MaxOccurancesAfk = 4;

    public void TrackPlayer(ulong steamId)
    {
        AfkTracker.TryAdd(steamId, 0);
    }

    public void RemovePlayer(ulong steamId)
    {
        AfkTracker.Remove(steamId);
    }

    public void Clear()
    {
        AfkTracker.Clear();
    }

    public void CheckPlayerStatus()
    {
        if (ServerHelper.IsPracMode || GameHelpers.IsWarmup())
            return;

        var validPlayers = GameHelpers
            .GetValidPlayers()
            .Where(player => player.IsBot == false)
            .Where(player => player.IsHLTV == false)
            .Where(player => player.Connected == PlayerConnectedState.Connected)
            .Where(player => player.PawnIsAlive)
            .Where(player => player.Team != CsTeam.Spectator)
            .ToList();

        Log.Info("Checking afk players...");

        foreach (var player in validPlayers)
        {
            if (!AfkTracker.ContainsKey(player.SteamID))
                continue;

            var afkCount = AfkTracker[player.SteamID];

            if (afkCount >= MaxOccurancesAfk)
            {
                // One final check in case they've come back
                if (AfkUtils.IsPressingAnyKey(player))
                {
                    return;
                }

                Log.Info($"Found {player.SteamID} {player.PlayerName} AFK.");

                Server.PrintToChatAll(
                    $" {ChatColors.Red} [MACROS]: >>> Kicked {player.PlayerName} for being AFK!"
                );
                Server.ExecuteCommand($"kickid {player.UserId} You have been kicked for being AFK");

                RemovePlayer(player.SteamID);
                continue;
            }

            if (AfkUtils.IsPressingAnyKey(player))
            {
                AfkTracker[player.SteamID] = 0;
                continue;
            }

            if (!AfkUtils.IsPressingAnyKey(player))
            {
                if (GameGuards.IsAdmin(player.SteamID.ToString()))
                    return;
                Log.Info($"{player.SteamID} {player.PlayerName} appears to be AFK...");
                AfkTracker[player.SteamID]++;

                player.PrintToChat(
                    $" {ChatColors.Red} [MACROS]: >>> You will be kicked if you are AFK!"
                );
            }
        }
    }
}
