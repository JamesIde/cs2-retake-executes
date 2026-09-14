using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public static class CommandRecordLastThrownSmoke
{
    public static void Execute(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }

        if (player == null || !player.IsValid)
            return;

        var projectiles = Utilities.FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>(
            "smokegrenade_projectile"
        );
        var smoke = projectiles.FirstOrDefault();

        if (smoke == null)
        {
            player.PrintToChat(" [Retakes] No smoke found, throw one first!");
            return;
        }

        var pos = smoke.AbsOrigin;
        var vel = smoke.AbsVelocity;
        var ang = smoke.AbsRotation;

        if (pos == null || vel == null || ang == null)
            return;

        player.PrintToChat($" [Smoke] Origin: {pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}");
        player.PrintToChat($" [Smoke] Velocity: {vel.X:F2}, {vel.Y:F2}, {vel.Z:F2}");
        player.PrintToChat($" [Smoke] Angle: {ang.X:F2}, {ang.Y:F2}, {ang.Z:F2}");

        Log.Info(
            $"{{\n"
                + $"  \"name\": \"\",\n"
                + $"  \"origin\": \"{pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}\",\n"
                + $"  \"velocity\": \"{vel.X:F2}, {vel.Y:F2}, {vel.Z:F2}\",\n"
                + $"  \"angle\": \"{ang.X:F2}, {ang.Y:F2}, {ang.Z:F2}\"\n"
                + $"}}"
        );
    }
}
