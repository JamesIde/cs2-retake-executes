using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class WeaponAllocationService(GameState _gameState)
{
    private readonly GameState gameState = _gameState;
    private static readonly Random random = new();

    #region Pistol Round
    private static readonly HashSet<CsItem> EligiblePistolUtility =
    [
        CsItem.HEGrenade,
        CsItem.Flashbang,
        CsItem.Molotov,
        CsItem.Incendiary,
    ];
    #endregion

    #region Non Stackable Utility
    private static readonly HashSet<CsItem> NonStackableUtility =
    [
        CsItem.HEGrenade,
        CsItem.Molotov,
        CsItem.Incendiary,
    ];
    #endregion

    #region Full Buy Round (AWP) CT
    private static readonly HashSet<CsItem> AWPFullBuyCT =
    [
        CsItem.AWP,
        CsItem.AssaultSuit,
        CsItem.USP,
    ];
    #endregion

    #region Full Buy Round (AWP) CT
    private static readonly HashSet<CsItem> AWPFullBuyT =
    [
        CsItem.AWP,
        CsItem.AssaultSuit,
        CsItem.Glock,
    ];
    #endregion

    private static readonly HashSet<CsItem> Pistols =
    [
        CsItem.FiveSeven,
        CsItem.Deagle,
        CsItem.Tec9,
        CsItem.Glock,
        CsItem.USP,
    ];

    #region Assigning Util & Weapons
    public void AssignPlayerWeaponsAndUtility(
        CCSPlayerController player,
        RoundType type,
        PlayerData playerData
    )
    {
        // Remove old weapons, assign knives & c4 back
        var IsBombCarrier = player.SteamID == gameState.SelectedBombCarrier;
        AssignPlayerDefaults(player, IsBombCarrier);

        var exclude = new HashSet<CsItem>();

        var preferences =
            player.Team == CsTeam.Terrorist ? playerData.TPreferences : playerData.CTPreferences;

        // https://github.com/B3none/cs2-retakes/blob/master/RetakesPlugin/Services/AllocationService.cs#L27
        if (
            player.Team == CsTeam.CounterTerrorist
            && player.PlayerPawn.IsValid
            && player.PlayerPawn.Value != null
            && player.PlayerPawn.Value.IsValid
            && player.PlayerPawn.Value.ItemServices != null
        )
        {
            // TODO: 50% roll here?
            // need to adjust bomb logic to see if the player has a kit

            var itemServices = new CCSPlayer_ItemServices(
                player.PlayerPawn.Value.ItemServices.Handle
            );
            itemServices.HasDefuser = true;
        }

        switch (type)
        {
            case RoundType.PistolRound:
                AssignPistolRound(player, preferences.PistolRoundWeapon);

                // Only frag or flash on pistol
                exclude.Add(CsItem.Molotov);
                exclude.Add(CsItem.Incendiary);

                var pUtil = DeterminePlayerUtility(player.Team, exclude);

                GivePlayerItem(player, pUtil);
                break;
            case RoundType.ForceBuy:
                Log.Info($"Force buy weapon is {preferences.ForceBuyWeapon}");
                AssignForceBuy(player, preferences.ForceBuyWeapon);

                var fUtility = DeterminePlayerUtility(
                    player.Team,
                    exclude,
                    Constants.PERCENTAGE_CHANCE_SECOND_GRENADE
                );

                GivePlayerItem(player, fUtility);
                break;
            default:
                AssignFullBuy(player, preferences.FullBuyWeapon, preferences.PistolRoundWeapon);

                var fbUtility = DeterminePlayerUtility(player.Team, exclude);
                GivePlayerItem(player, fbUtility);
                break;
        }
    }

    #region Assign Pistol Round
    private static void AssignPistolRound(CCSPlayerController player, CsItem pistol)
    {
        GivePlayerItem(player, [CsItem.Kevlar, pistol]);
    }
    #endregion

    #region  Assign Force Buy
    private static void AssignForceBuy(CCSPlayerController player, CsItem item)
    {
        HashSet<CsItem> baseSet = [CsItem.AssaultSuit];

        if (player.Team == CsTeam.Terrorist)
        {
            baseSet.UnionWith([item]);

            if (!Pistols.Contains(item))
                baseSet.UnionWith([CsItem.Glock]);

            GivePlayerItem(player, baseSet);
            return;
        }

        baseSet.UnionWith([item]);

        if (!Pistols.Contains(item))
            baseSet.UnionWith([CsItem.USP]);

        GivePlayerItem(player, baseSet);
    }
    #endregion

    #region Assign Full Buy
    private void AssignFullBuy(CCSPlayerController player, CsItem item, CsItem? pistolOverride)
    {
        if (player.SteamID == gameState.SelectedCTAwper)
        {
            GivePlayerItem(player, AWPFullBuyCT);
            return;
        }

        if (player.SteamID == gameState.SelectedTAwper)
        {
            GivePlayerItem(player, AWPFullBuyT);
            return;
        }

        HashSet<CsItem> baseSet = [CsItem.AssaultSuit];

        if (player.Team == CsTeam.Terrorist)
        {
            baseSet.UnionWith([item, pistolOverride ?? CsItem.Glock]);
            GivePlayerItem(player, baseSet);
            return;
        }

        baseSet.UnionWith([item, pistolOverride ?? CsItem.USP]);
        GivePlayerItem(player, baseSet);
    }
    #endregion

    #region Determine Utility
    /// <summary>
    /// Determines the player the utility for the round.
    /// A player at most can have two pieces of utilty. 100% chance of one piece, X% chance of second piece driven via config.
    /// If the first piece is non-stackable (i.e. is anything but a flashbang), that item is removed from the potential pool of next pieces of utilty.
    /// This is so we don't try stack two of something that can't! It would be dropped on the ground.
    /// There is also a check if the team matches the item selected. I.e. CT = incendiary, T = molotov.
    /// </summary>
    /// <param name="team"></param>
    /// <returns></returns>
    private List<CsItem> DeterminePlayerUtility(
        CsTeam team,
        HashSet<CsItem> itemsToExclude,
        double percentageChanceSecondGrenade = 0f
    )
    {
        List<CsItem> calculatedItems = [];

        var pool = EligiblePistolUtility.Except(itemsToExclude).ToHashSet();

        var firstItem = WeaponHelper.CheckIfTeamMatchesItem(
            pool.OrderBy(_ => random.Next()).First(),
            team
        );

        calculatedItems.Add(firstItem);

        if (WeaponHelper.ShouldRoll(percentageChanceSecondGrenade))
        {
            if (NonStackableUtility.Contains(firstItem))
                pool.Remove(firstItem);

            var secondItem = WeaponHelper.CheckIfTeamMatchesItem(
                pool.OrderBy(_ => random.Next()).First(),
                team
            );

            calculatedItems.Add(secondItem);
        }

        return calculatedItems;
    }
    #endregion

    #region Give Player Utilities
    private static void GivePlayerItem(CCSPlayerController player, ICollection<CsItem> items)
    {
        foreach (var item in items)
        {
            player.GiveNamedItem(item);
        }
    }
    #endregion

    private static void AssignPlayerDefaults(CCSPlayerController player, bool IsBombCarrier)
    {
        player.RemoveWeapons();
        if (player.Team == CsTeam.Terrorist)
        {
            player.GiveNamedItem(CsItem.DefaultKnifeT);

            if (IsBombCarrier)
            {
                player.GiveNamedItem(CsItem.C4);
            }
            return;
        }

        player.GiveNamedItem(CsItem.DefaultKnifeCT);
    }
    #endregion
}
