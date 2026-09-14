using CounterStrikeSharp.API.Modules.Commands;
using RetakeExecutesPlugin;

/// <summary>
/// Constructor command Manager.
/// Fiddly way of registering / deregistering commands to ensure 'state' is not carried over between maps, causing commands to bug out.
/// </summary>
/// <param name="_plugin"></param>
/// <param name="_utilService"></param>
/// <param name="_gameService"></param>
public class CtorCommandManager(
    RetakeExecutes _plugin,
    UtilService _utilService,
    GameService _gameService,
    PlayerDataService _playerDataService,
    SkinChangerService _skinChangerService,
    MapVotingService _mapVoteService
)
{
    private readonly RetakeExecutes plugin = _plugin;
    private readonly UtilService utilService = _utilService;
    private readonly GameService gameService = _gameService;
    private readonly PlayerDataService playerDataService = _playerDataService;
    private readonly SkinChangerService skinChangerService = _skinChangerService;
    private readonly MapVotingService mapVotingService = _mapVoteService;

    // 1. Create our command callback
    private CommandInfo.CommandCallback? _rethrowHandler;
    private CommandInfo.CommandCallback? _scrambleMapHandler;
    private CommandInfo.CommandCallback? _reloadPreferencesHandler;
    private CommandInfo.CommandCallback _mapVoteHandler;
    private CommandInfo.CommandCallback _kickHandler;
    private CommandInfo.CommandCallback _banHandler;

    public void RegisterCommands()
    {
        // 2. Create our instances of the commands we need
        var commandRethrowRoundUtility = new CommandRethrowRoundUtility(utilService);
        var commandScrambleMap = new CommandScrambleMap(gameService);
        var commandReloadPreferences = new CommandReloadPreferences(
            playerDataService,
            skinChangerService
        );

        var commandMapVote = new CommandMapVote(mapVotingService);
        var kickBanVote = new CommandKickBan(new KickBanService(_plugin, playerDataService)); // TODO KickBan thru primary constructor

        // 3. Assign it to the command callback
        _rethrowHandler = commandRethrowRoundUtility.Execute;
        _scrambleMapHandler = commandScrambleMap.Execute;
        _reloadPreferencesHandler = commandReloadPreferences.Execute;
        _mapVoteHandler = commandMapVote.Execute;
        _kickHandler = kickBanVote.ExecuteKick;
        _banHandler = kickBanVote.ExecuteBan;

        // 4. Register it in the command
        plugin.AddCommand("css_rt", "Rethrows round utility", _rethrowHandler);
        plugin.AddCommand("css_scrambleMap", "Scramble the map", _scrambleMapHandler);
        plugin.AddCommand("css_reload", "Reload weapon preferences", _reloadPreferencesHandler);
        plugin.AddCommand("css_rtv", "Map Vote", _mapVoteHandler);
        plugin.AddCommand("css_kick", "Kick Player", _kickHandler);
        plugin.AddCommand("css_ban", "Ban Player", _banHandler);
    }

    public void DeregisterCommands()
    {
        // 5. Remove the command
        plugin.RemoveCommand("css_rt", _rethrowHandler);
        plugin.RemoveCommand("css_scrambleMap", _scrambleMapHandler);
        plugin.RemoveCommand("css_reload", _reloadPreferencesHandler);
        plugin.RemoveCommand("css_rtv", _mapVoteHandler);
        plugin.RemoveCommand("css_kick", _kickHandler);
        plugin.RemoveCommand("css_ban", _banHandler);

        // 6. Set the command callback to null
        _rethrowHandler = null;
        _scrambleMapHandler = null;
        _reloadPreferencesHandler = null;
        _mapVoteHandler = null;
        _kickHandler = null;
        _banHandler = null;
    }
}
