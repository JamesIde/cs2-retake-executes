using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class WeaponHelper
{
    private static readonly Random random = new();

    public static CsItem CheckIfTeamMatchesItem(CsItem item, CsTeam team)
    {
        return item switch
        {
            CsItem.Incendiary when team == CsTeam.Terrorist => CsItem.Molotov,
            CsItem.Molotov when team == CsTeam.CounterTerrorist => CsItem.Incendiary,
            _ => item,
        };
    }

    public static bool ShouldRoll(double percentageChance)
    {
        return random.NextDouble() < percentageChance;
    }
}
