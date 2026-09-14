using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class TeleportService()
{
    public void TeleportPlayer(CCSPlayerController? player, SpawnData spawn)
    {
        var pawn = player?.PlayerPawn?.Value;
        if (pawn is null)
        {
            Log.Info(
                $"Player {player?.PlayerName} pawn value is empty. Can't teleport to {spawn.Label}"
            );
            return;
        }

        pawn.Teleport(spawn.Origin, spawn.Angle, new Vector(0, 0, 0));

        Log.Info($"Teleported {player?.PlayerName} to {spawn.Label}.");
    }
}
