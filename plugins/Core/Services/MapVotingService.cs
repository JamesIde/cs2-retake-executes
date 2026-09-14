using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class MapVotingService(BasePlugin _plugin, GameState _gameState)
{
    private readonly BasePlugin plugin = _plugin;
    private readonly GameState gameState = _gameState;

    private readonly HashSet<ulong> voters = [];
    private readonly Dictionary<string, int> votes = [];
    private readonly Dictionary<string, string> maps = new()
    {
        ["Dust 2"] = "de_dust2",
        ["Mirage"] = "de_mirage",
        ["Cache"] = "de_cache",
        ["Inferno"] = "de_inferno",
        ["Ancient"] = "de_ancient",
        ["Train"] = "de_train",
        ["Nuke"] = "de_nuke",
        ["Anubis"] = "de_anubis",
        ["Overpass"] = "de_overpass",
    };
    private bool IsVotingInProgress = false;

    public void RemoveFromVoteList(ulong steamId)
    {
        if (voters.TryGetValue(steamId, out ulong _))
        {
            voters.Remove(steamId);
        }
    }

    public void ClearVoteList()
    {
        voters.Clear();
    }

    public void RockTheVote(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid)
            return;

        if (IsVotingInProgress)
        {
            command.ReplyToCommand("Map voting is currently in progress.");
            return;
        }

        bool added = voters.Add(player.SteamID);
        if (!added)
        {
            command.ReplyToCommand("You have already voted.");
            return;
        }

        if (!string.IsNullOrEmpty(gameState.RtvMidGameNextMap))
        {
            command.ReplyToCommand("Map has already been rtv'd.");
            Log.Info($"HERE RTV'd MAP {gameState.RtvMidGameNextMap}");
            return;
        }

        if (!string.IsNullOrEmpty(gameState.EndGameNextMap))
        {
            command.ReplyToCommand("Next map has already been selected.");
            return;
        }

        CheckVoteResult();
    }

    public void CheckVoteResult()
    {
        int connected = GetConnectedPlayers().Count;
        if (connected == 0)
            return;

        int required = (int)Math.Ceiling(connected * 0.6);

        if (voters.Count >= required)
        {
            Server.PrintToChatAll("Rock the vote initiated.");
            ClearVoteList();
            StartMapVote();
            IsVotingInProgress = true;
        }
        else
        {
            Server.PrintToChatAll(
                $"Player wants to rock the vote. Requires {ChatColors.Green} {required - voters.Count} {ChatColors.White} more votes"
            );
        }
    }

    public void StartMapVote(bool isEndGameVoting = false)
    {
        var allPlayers = GetConnectedPlayers();

        var menu = new CenterHtmlMenu("Vote for the next map!", plugin);

        foreach (var map in maps.Keys)
        {
            AddOption(menu, map);
        }

        foreach (var player in allPlayers)
        {
            MenuManager.OpenCenterHtmlMenu(plugin, player, menu);
        }

        plugin.AddTimer(30f, () => ProcessVotingResults(isEndGameVoting));

        void AddOption(CenterHtmlMenu menu, string option)
        {
            menu.AddMenuOption(
                option,
                (p, opt) =>
                {
                    votes.TryGetValue(opt.Text, out var count);
                    votes[opt.Text] = count + 1;

                    MenuManager.CloseActiveMenu(p);
                    Server.PrintToChatAll(
                        $"{p.PlayerName} voted for {opt.Text} ({votes[opt.Text]} votes)"
                    );
                }
            );
        }
    }

    public void ProcessVotingResults(bool isEndGameVoting)
    {
        Server.PrintToChatAll(new string('-', 60));
        Server.PrintToChatAll("Voting has finished.");

        if (votes.Count == 0)
        {
            Server.PrintToChatAll("No votes received for any map.");
            votes.Clear();
            ClearVoteList();
            IsVotingInProgress = false;
            Server.PrintToChatAll(new string('-', 60));
            return;
        }

        var selectedMap = votes
            .OrderByDescending(vote => vote.Value)
            .Take(1)
            .Select(vote => vote.Key)
            .FirstOrDefault();

        Server.PrintToChatAll(
            $" {ChatColors.Green} {selectedMap} {ChatColors.White} has been selected as next map."
        );

        IsVotingInProgress = false;
        votes.Clear();
        ClearVoteList();
        Server.PrintToChatAll(new string('-', 60));

        if (isEndGameVoting)
        {
            gameState.EndGameNextMap = maps[selectedMap];
        }
        else
        {
            gameState.RtvMidGameNextMap = maps[selectedMap];
        }
    }

    public static List<CCSPlayerController> GetConnectedPlayers()
    {
        return
        [
            .. Utilities
                .GetPlayers()
                .Where(p =>
                    p.IsValid
                    && !p.IsBot
                    && !p.IsHLTV
                    && p.Connected == PlayerConnectedState.Connected & p.Team != CsTeam.Spectator
                ),
        ];
    }
}
