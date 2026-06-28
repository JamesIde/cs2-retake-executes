using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public class CommandRethrowRoundUtility(UtilService _utilityService)
{
    private readonly UtilService utilService = _utilityService;

    public void Execute(CCSPlayerController? player, CommandInfo command)
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

        if (!int.TryParse(input, out int roundId))
        {
            command.ReplyToCommand("Could not parse input to integer");
            return;
        }

        utilService.RethrowRoundUtility(roundId);
    }
}
