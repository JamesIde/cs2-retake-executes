using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public static class CommandGetPosition
{
    public static void Execute(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }

        var pos = player.PlayerPawn.Value?.AbsOrigin;
        var ang = player.PlayerPawn.Value?.AbsRotation;

        if (pos == null || ang == null)
            return;

        player.PrintToChat($" [Pos] X: {pos.X:F2} Y: {pos.Y:F2} Z: {pos.Z:F2}");
        player.PrintToChat($" [Ang] Pitch: {ang.X:F2} Yaw: {ang.Y:F2} Roll: {ang.Z:F2}");

        Log.Info(
            $"{{\n"
                + $"  \"spawnId\": ,\n"
                + $"  \"label\": \"\",\n"
                + $"  \"team\": \"\",\n"
                + $"  \"origin\": \"{pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}\",\n"
                + $"  \"angle\": \"{ang.X:F2}, {ang.Y:F2}, {ang.Z:F2}\"\n"
                + $"}}"
        );
    }
}
