using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class CommandReloadPreferences(
    PlayerDataService _playerData,
    SkinChangerService _skinChanger
)
{
    private readonly PlayerDataService playerDataService = _playerData;
    private readonly SkinChangerService skinChanger = _skinChanger;

    public void Execute(CCSPlayerController? player, CommandInfo command)
    {
        var steamId = player.SteamID;
        Task.Run(async () =>
        {
            await playerDataService.ReloadPlayerData(steamId);
            await playerDataService.RetrievePlayerSkins(steamId);

            playerDataService.PrintPlayerData();

            await Server.NextFrameAsync(() =>
            {
                skinChanger.ApplyKnifeAndGloves(steamId);
                // skinChanger.ApplyPlayerSkins(steamId, RoundType.None);

                player.PrintToChat(
                    $" {ChatColors.Gold}[MACROS]: >>> {ChatColors.White}Weapon preferences updated. They will take effect next round."
                );
            });
        });
    }
}
