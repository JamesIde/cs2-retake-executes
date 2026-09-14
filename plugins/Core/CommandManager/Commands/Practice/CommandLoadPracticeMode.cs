using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public static class CommandLoadPracticeMode
{
    public static void Execute(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }
        ServerHelper.ExecutePracticeConfig();
    }
}
