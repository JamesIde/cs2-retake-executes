using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public static class CommandChangeMap
{
    public static void Execute(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }

        string input = command.ArgString;

        if (string.IsNullOrEmpty(input))
        {
            command.ReplyToCommand("No input provided.");
            return;
        }

        string map = "de_" + input;

        MapConfiguration.Empty();
        Server.ExecuteCommand($"map {map}");
    }
}
