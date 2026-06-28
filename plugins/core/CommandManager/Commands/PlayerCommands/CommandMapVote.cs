using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public class CommandMapVote(MapVotingService _mapVoteService)
{
    private readonly MapVotingService mapVoteService = _mapVoteService;

    public void Execute(CCSPlayerController? player, CommandInfo command)
    {
        mapVoteService.RockTheVote(player, command);
    }
}
