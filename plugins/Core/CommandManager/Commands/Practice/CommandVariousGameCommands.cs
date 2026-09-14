using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class CommandVariousGameCommands
{
    public static void CommandExtendWarmup(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }
        Server.PrintToChatAll(
            $" {ChatColors.LightPurple} Map staying in warmup until a player joins..."
        );
        Server.ExecuteCommand("mp_warmup_start");
        Server.ExecuteCommand("mp_warmup_pausetimer 1");
    }

    public static void CommandEndWarmup(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }
        Server.PrintToChatAll($" {ChatColors.LightPurple} [MACROS]: >>> Ending warmup");
        Server.ExecuteCommand("mp_warmup_end");
    }

    public static void CommandRestartGame(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }

        Server.PrintToChatAll($" {ChatColors.LightPurple} [MACROS]: >>> Restarting Game");
        Server.ExecuteCommand("mp_restartgame 1");
    }
}
