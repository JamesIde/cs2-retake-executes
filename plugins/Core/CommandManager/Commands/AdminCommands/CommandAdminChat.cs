using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class CommandAdminChat
{
    public static void OnAdminSay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission to use this command.");
            return;
        }

        Server.PrintToChatAll($" {ChatColors.Red}[ADMIN]{ChatColors.White} {command.ArgString}");
    }
}
