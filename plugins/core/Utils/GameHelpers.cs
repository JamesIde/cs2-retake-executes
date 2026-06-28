using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class GameHelpers
{
    private static readonly Random random = new();

    public static bool IsWarmup()
    {
        var rules = Utilities
            .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault();
        return rules?.GameRules?.WarmupPeriod ?? false;
    }

    public static CCSGameRules? GetGameRules()
    {
        return Utilities
            .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()
            ?.GameRules;
    }

    public static bool IsPlayerValid(CCSPlayerController? player)
    {
        return player == null || !player.IsValid || !player.PawnIsAlive;
    }

    // TODO - is there a better way?
    public static string MapCSSTeamToString(CsTeam team)
    {
        return team switch
        {
            CsTeam.Terrorist => "Terrorist",
            CsTeam.CounterTerrorist => "Counter Terrorist",
            CsTeam.Spectator => "Spectator",
            _ => "None",
        };
    }

    public static string MapRoundTypeToString(RoundType? type)
    {
        return type switch
        {
            RoundType.PistolRound => "Pistols Only",
            RoundType.ForceBuy => "Force Buy",
            RoundType.FullBuy => "Full Buy",
            _ => throw new InvalidOperationException("No round type is active"),
        };
    }

    public static List<CCSPlayerController> GetValidPlayers()
    {
        return
        [
            .. Utilities
                .GetPlayers()
                .Where(p => p.IsValid && p.PlayerPawn.IsValid && !p.IsBot && !p.IsHLTV),
        ];
    }

    public static T PickRandom<T>(List<T> items)
    {
        if (items.Count == 0)
        {
            ArgumentNullException.ThrowIfNull(items);
        }

        new Queue<T>(items.OrderBy(_ => random.Next())).TryDequeue(out T item);
        return item!;
    }

    public static CsTeam GetRoundWinner(int winner) =>
        winner switch
        {
            2 => CsTeam.Terrorist,
            3 => CsTeam.CounterTerrorist,
            _ => CsTeam.None,
        };

    public static string GetRoundWinnerString(int winner) =>
        winner switch
        {
            2 => "t",
            3 => "ct",
            _ => throw new InvalidDataException(),
        };

    public static float DistanceTo(Vector a, Vector b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
