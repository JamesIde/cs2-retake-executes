using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public class CommandScrambleMap(GameService _gameService)
{
    private readonly GameService gameService = _gameService;

    public void Execute(CCSPlayerController? player, CommandInfo command)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            command.ReplyToCommand("You do not have permission make this command.");
            return;
        }
        gameService.SelectAndLoadNextMap();
    }
}
