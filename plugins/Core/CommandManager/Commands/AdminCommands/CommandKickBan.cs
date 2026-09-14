using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public class CommandKickBan(KickBanService _kickBanService)
{
    private readonly KickBanService kickBanService = _kickBanService;

    public void ExecuteKick(CCSPlayerController? player, CommandInfo command)
    {
        kickBanService.KickPlayer(player, command);
    }

    public void ExecuteBan(CCSPlayerController? player, CommandInfo command)
    {
        kickBanService.BanPlayer(player, command);
    }
}
