using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public static class CommandAddBot
{
    public static void ExecuteT(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }

        Server.ExecuteCommand("bot_add_t");
        Server.PrintToChatAll("[MACROS]: >>> Added T bot");
    }

    public static void ExecuteCT(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            return;
        }
        Server.ExecuteCommand("bot_add_ct");
        Server.PrintToChatAll("[MACROS]: >>> Added CT bot");
    }
}
