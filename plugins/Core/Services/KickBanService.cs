using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace RetakeExecutesPlugin;

public class KickBanService(BasePlugin _plugin, PlayerDataService _playerDataService)
{
    private readonly BasePlugin plugin = _plugin;
    private readonly PlayerDataService playerDataService = _playerDataService;

    #region Kick Player
    public void KickPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (!Constants.ADMIN_STEAM_ID_64.Contains(player.SteamID.ToString()))
            return;

        Dictionary<string, CCSPlayerController> currentPlayers = [];

        var players = Utilities.GetPlayers().Where(p => !p.IsBot).Where(p => !p.IsHLTV).ToList();

        var menu = new CenterHtmlMenu("[ADMIN ONLY] Kick Players", plugin);

        foreach (var playerController in players)
        {
            currentPlayers.Add(playerController.PlayerName, playerController);
            AddOption(menu, playerController.PlayerName);
        }

        MenuManager.OpenCenterHtmlMenu(plugin, player, menu);

        void AddOption(CenterHtmlMenu menu, string option)
        {
            menu.AddMenuOption(
                option,
                (p, opt) =>
                {
                    currentPlayers.TryGetValue(opt.Text, out var player);

                    MenuManager.CloseActiveMenu(p);

                    player!.PrintToChat(
                        $"Selected {player.PlayerName} | {player.SteamID} to be kicked."
                    );

                    Server.ExecuteCommand(
                        $"kickid {player.UserId} You have been kicked by an admin."
                    );
                }
            );
        }
    }
    #endregion

    #region Ban Player
    public void BanPlayer(CCSPlayerController? player, CommandInfo command)
    {
        if (!Constants.ADMIN_STEAM_ID_64.Contains(player.SteamID.ToString()))
            return;

        Dictionary<string, CCSPlayerController> currentPlayers = [];

        var players = Utilities.GetPlayers().Where(p => !p.IsBot).Where(p => !p.IsHLTV).ToList();

        var menu = new CenterHtmlMenu("[ADMIN ONLY] Ban Players", plugin);

        foreach (var playerController in players)
        {
            currentPlayers.Add(playerController.PlayerName, playerController);
            AddPlayerOption(menu, playerController.PlayerName);
        }

        MenuManager.OpenCenterHtmlMenu(plugin, player, menu);

        void AddPlayerOption(CenterHtmlMenu menu, string option)
        {
            menu.AddMenuOption(
                option,
                (p, opt) =>
                {
                    currentPlayers.TryGetValue(opt.Text, out var target);

                    if (target == null || !target.IsValid)
                    {
                        p.PrintToChat("Selected player is no longer available.");
                        MenuManager.CloseActiveMenu(p);
                        return;
                    }

                    OpenDurationMenu(p, target);
                }
            );
        }

        void OpenDurationMenu(CCSPlayerController admin, CCSPlayerController target)
        {
            var durations = new[] { 1, 3, 7, 30, 9999 };

            var durationMenu = new CenterHtmlMenu(
                $"[ADMIN ONLY] Duration for {target.PlayerName}",
                plugin
            );

            foreach (var days in durations)
            {
                durationMenu.AddMenuOption(
                    days == 9999 ? "Permanent (9999 days)" : $"{days} day{(days > 1 ? "s" : "")}",
                    (p, opt) =>
                    {
                        MenuManager.CloseActiveMenu(p);

                        if (!target.IsValid)
                        {
                            p.PrintToChat("Selected player is no longer available.");
                            return;
                        }

                        Task.Run(async () =>
                        {
                            try
                            {
                                var playerBan = new PlayerBan
                                {
                                    BanExpiresAt =
                                        days == 999
                                            ? DateTime.UtcNow.AddYears(10)
                                            : DateTime.UtcNow.AddDays(days),
                                    SteamId = target.SteamID,
                                };

                                await playerDataService.BanPlayer(target.SteamID, playerBan);

                                Server.NextFrame(() =>
                                {
                                    Server.ExecuteCommand($"kickid {target.UserId}");
                                    Server.PrintToChatAll(
                                        $"{target.PlayerName} | {target.SteamID} banned for {days} day(s)."
                                    );
                                });
                            }
                            catch (Exception ex)
                            {
                                DbExceptionHandler.Handle(ex);
                                throw;
                            }
                        });
                    }
                );
            }

            MenuManager.OpenCenterHtmlMenu(plugin, admin, durationMenu);
        }
    }
    #endregion

    #region Check Ban On Load
    public void CheckPlayerBans(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        var playerName = player.PlayerName;
        var userId = player.UserId;

        Task.Run(async () =>
        {
            try
            {
                var bans = await playerDataService.RetrievePlayerBans(steamId);

                Log.Info($"BANS FOR PLAYER {bans.Count}");

                if (bans.Count == 0)
                    return;

                var latestBan = bans.OrderByDescending(p => p.BanExpiresAt).FirstOrDefault();

                if (latestBan is null)
                    return;

                Log.Info($"FOUND LATEST BAN {latestBan.BanExpiresAt}");

                if (DateTime.UtcNow < latestBan.BanExpiresAt)
                {
                    Log.Info($"Player has an active ban. Kicking. {playerName} | {steamId}");

                    Server.NextFrame(() =>
                    {
                        if (userId.HasValue)
                            Server.ExecuteCommand($"kickid {userId.Value}");
                    });
                }
            }
            catch (Exception ex)
            {
                DbExceptionHandler.Handle(ex);
            }
        });
    }
    #endregion
}
