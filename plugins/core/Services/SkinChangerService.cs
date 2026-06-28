using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class SkinChangerService(
    BasePlugin _plugin,
    PlayerDataService _playerDataService,
    GameState _gameState
)
{
    private readonly PlayerDataService playerDataService = _playerDataService;
    private readonly BasePlugin plugin = _plugin;
    private readonly GameState gameState = _gameState;
    private static ulong _nextItemId = 10_000;

    private static readonly HashSet<CsItem> ForceBuyPrimaries =
    [
        CsItem.Mac10,
        CsItem.UMP45,
        CsItem.MP7,
        CsItem.MP9,
    ];

    private static readonly MemoryFunctionWithReturn<IntPtr, string, float, IntPtr> AttributeSet =
        new(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "40 53 55 41 56 48 81 EC 90 00 00 00"
                : "55 48 89 E5 41 57 41 56 49 89 FE 41 55 41 54 53 48 89 F3 48 83 EC ? F3 0F 11 85"
        );

    public void ApplyPlayerSkins(ulong steamId, RoundType roundType)
    {
        var playerData = playerDataService.playerData[steamId];

        if (playerData is null)
        {
            Log.Info($"Player data info is null for user {steamId}");
            return;
        }

        if (playerData.PlayerSkinPreferences.Count == 0)
        {
            Log.Info($"No skins recorded for player {steamId}. Ignoring");
            return;
        }

        var player = Utilities.GetPlayerFromSteamId(steamId);

        var playerSkinDefIndexMap = playerData
            .PlayerSkinPreferences.Where(p => p.IsWeapon)
            .Where(p => ParseTeam(p.Team) == player.Team)
            .ToDictionary(k => k.WeaponIndex);

        // var map = playerData.PlayerSkinPreferences.ToL

        foreach (var (key, value) in playerSkinDefIndexMap)
        {
            // Log.Info($"Skin Def Index Map {key} | {value.IsLegacyModel}");
        }

        var preferences =
            player?.Team == CsTeam.Terrorist ? playerData.TPreferences : playerData.CTPreferences;

        var pistolWeapon = ParsePistolPreference(preferences);

        // Easiest is pistol to start
        if (roundType == RoundType.PistolRound)
        {
            MatchAndApply(pistolWeapon, playerSkinDefIndexMap, player);
        }
        else if (roundType == RoundType.FullBuy)
        {
            // First apply the pistol weapon skin if applicable
            MatchAndApply(pistolWeapon, playerSkinDefIndexMap, player);

            if (IsAwper(steamId, player.Team))
            {
                Log.Info($"Player {steamId} is awper. Checking if they have a selected awp skin");
                MatchAndApply(CsItem.AWP.ToWeaponString(), playerSkinDefIndexMap, player);
            }
            else
            {
                MatchAndApply(
                    preferences.FullBuyWeapon.ToWeaponString(),
                    playerSkinDefIndexMap,
                    player
                );
            }
        }
        else if (roundType == RoundType.ForceBuy)
        {
            // Force buy primaries will have a secondary (give by a separate service)
            // If theres a corresponding secondary skin, we want to apply it.
            // Otherwise we just apply the pistol (tec9, deagle, 57)
            if (ForceBuyPrimaries.Contains(preferences.ForceBuyWeapon))
            {
                MatchAndApply(
                    preferences.ForceBuyWeapon.ToWeaponString(),
                    playerSkinDefIndexMap,
                    player
                );
                MatchAndApply(pistolWeapon, playerSkinDefIndexMap, player);
            }
            else
            {
                // Apply tec9, deagle, 57
                MatchAndApply(
                    preferences.ForceBuyWeapon.ToWeaponString(),
                    playerSkinDefIndexMap,
                    player
                );
            }
        }
        else
        {
            MatchAndApply(pistolWeapon, playerSkinDefIndexMap, player);
        }
    }

    private bool IsAwper(ulong steamId, CsTeam team)
    {
        if (steamId == gameState.SelectedTAwper && team == CsTeam.Terrorist)
            return true;
        if (steamId == gameState.SelectedCTAwper && team == CsTeam.CounterTerrorist)
            return true;
        return false;
    }

    private string ParsePistolPreference(WeaponPreferences preference)
    {
        if (
            preference.PistolRoundWeapon == CsItem.Glock18
            || preference.PistolRoundWeapon == CsItem.Glock
        )
        {
            return "weapon_glock";
        }
        return preference.PistolRoundWeapon.ToWeaponString();
    }

    private static readonly Dictionary<ushort, ushort> PairedDefIndex = new()
    {
        { 61, 32 },
        { 32, 61 },
        { 60, 16 },
        { 16, 60 },
    };

    private static CBasePlayerWeapon? FindByDefIndex(
        CPlayer_WeaponServices services,
        ushort defIndex
    )
    {
        foreach (var handle in services.MyWeapons)
        {
            var w = handle.Value;
            if (w != null && w.IsValid && w.AttributeManager.Item.ItemDefinitionIndex == defIndex)
                return w;
        }
        return null;
    }

    private static string ResolveGiveName(ushort defIndex, string designerName) =>
        defIndex switch
        {
            60 => "weapon_m4a1_silencer",
            61 => "weapon_usp_silencer",
            _ => designerName,
        };

    public void MatchAndApply(
        string weaponName,
        Dictionary<ushort, PlayerSkin> skinDefIndexMap,
        CCSPlayerController? player
    )
    {
        if (player == null || !player.IsValid)
            return;

        if (!DefIndexMap.WeaponDefIndex.TryGetValue(weaponName, out ushort defIndex))
        {
            Log.Warn($"No defindex for weapon string '{weaponName}'");
            return;
        }

        if (!skinDefIndexMap.TryGetValue(defIndex, out PlayerSkin? skin) || skin == null)
            return;

        Log.Info(
            $"def {defIndex} paint {skin.WeaponIndex} legacy={skin.IsLegacyModel} -> body,{(skin.IsLegacyModel ? 1 : 0)}"
        );

        var weaponServices = player.PlayerPawn.Value?.WeaponServices;
        if (weaponServices == null)
            return;

        var target = FindByDefIndex(weaponServices, defIndex);

        if (target == null && PairedDefIndex.TryGetValue(defIndex, out ushort partnerDef))
            target = FindByDefIndex(weaponServices, partnerDef);

        if (target == null)
        {
            Log.Info($"{weaponName} (def {defIndex}) not in inventory, skipping");
            return;
        }

        if (target.AttributeManager.Item.ItemDefinitionIndex != defIndex)
        {
            target.AcceptInput("ChangeSubclass", value: defIndex.ToString());
            target.AttributeManager.Item.ItemDefinitionIndex = defIndex;

            var settledTarget = target;
            Server.NextFrame(() =>
            {
                if (settledTarget == null || !settledTarget.IsValid)
                    return;
                ApplySkin(
                    player,
                    settledTarget,
                    skin.WeaponPaintIndex,
                    skin.WeaponFloat,
                    skin.Seed,
                    skin.IsLegacyModel
                );
            });
            return;
        }

        Log.Info($"Matched: {weaponName} | {skin.WeaponPaintIndex} | {skin.WeaponName}");

        var active = weaponServices.ActiveWeapon.Value;
        bool isActive = active != null && active.IsValid && active.Index == target.Index;

        if (isActive)
            RefreshActiveWeapon(
                player,
                target,
                skin.WeaponPaintIndex,
                skin.WeaponFloat,
                skin.Seed,
                skin.IsLegacyModel
            );
        else
            ApplySkin(
                player,
                target,
                skin.WeaponPaintIndex,
                skin.WeaponFloat,
                skin.Seed,
                skin.IsLegacyModel
            );
    }

    #region Apply Knife and Gloves
    public void ApplyKnifeAndGloves(ulong steamId)
    {
        var playerData = playerDataService.playerData[steamId];

        if (playerData is null)
        {
            Log.Info($"Player data info is null for user {steamId}");
            return;
        }

        if (playerData.PlayerSkinPreferences.Count == 0)
        {
            Log.Info($"No skins recorded for player {steamId}. Ignoring");
            return;
        }

        var player = Utilities.GetPlayerFromSteamId(steamId);

        if (!player.IsValid || !player.PawnIsAlive)
        {
            Log.Info($"Player pawn not valid");
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            Log.Info($"Player pawn not valid");
            return;
        }

        var gloveRecord = playerData.PlayerSkinPreferences.FirstOrDefault(skin =>
            skin.IsGlove && ParseTeam(skin.Team) == player.Team
        );

        var knifeRecord = playerData.PlayerSkinPreferences.FirstOrDefault(skin =>
            skin.IsKnife && ParseTeam(skin.Team) == player.Team
        );

        if (gloveRecord is not null)
        {
            GiveGloves(
                player,
                gloveRecord.WeaponIndex,
                gloveRecord.WeaponPaintIndex,
                gloveRecord.WeaponFloat,
                gloveRecord.Seed
            );
        }

        if (knifeRecord is not null)
        {
            GiveKnife(
                player,
                knifeRecord.WeaponIndex,
                knifeRecord.WeaponPaintIndex,
                knifeRecord.WeaponFloat,
                knifeRecord.Seed
            );
        }
    }
    #endregion

    #region Give Gloves
    private void GiveGloves(
        CCSPlayerController player,
        ushort defIndex,
        int paintKit = 0,
        float wear = 0.01f,
        int seed = 0
    )
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        CEconItemView item = pawn.EconGloves;

        item.NetworkedDynamicAttributes.Attributes.RemoveAll();
        item.AttributeList.Attributes.RemoveAll();

        player.ExecuteClientCommand("lastinv");

        plugin.AddTimer(
            0.08f,
            () =>
            {
                if (!player.IsValid || !player.PawnIsAlive)
                    return;

                item.ItemDefinitionIndex = defIndex;
                UpdateItemId(item);

                item.NetworkedDynamicAttributes.Attributes.RemoveAll();
                item.AttributeList.Attributes.RemoveAll();

                if (paintKit > 0)
                    WriteAttributes(item, paintKit, wear, seed);

                item.Initialized = true;

                player.ExecuteClientCommand("lastinv");
                SetBodygroup(pawn, "first_or_third_person", 0);
                plugin.AddTimer(
                    0.2f,
                    () => SetBodygroup(pawn, "first_or_third_person", 1),
                    TimerFlags.STOP_ON_MAPCHANGE
                );
            },
            TimerFlags.STOP_ON_MAPCHANGE
        );
    }
    #endregion


    #region Give Knife
    private void GiveKnife(
        CCSPlayerController player,
        ushort defIndex,
        int paintKit = 0,
        float wear = 0.01f,
        int seed = 0
    )
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null)
            return;

        foreach (var handle in pawn.WeaponServices.MyWeapons)
        {
            var w = handle.Value;
            if (w == null || !w.IsValid)
                continue;
            if (w.DesignerName.Contains("knife") || w.DesignerName.Contains("bayonet"))
                w.AddEntityIOEvent("Kill", w, null, "", 0.0f);
        }

        plugin.AddTimer(
            0.1f,
            () =>
            {
                if (!player.IsValid || !player.PawnIsAlive)
                    return;

                var knife = new CBasePlayerWeapon(player.GiveNamedItem(CsItem.Knife));
                if (knife == null || !knife.IsValid)
                    return;

                knife.AcceptInput("ChangeSubclass", value: defIndex.ToString());

                Server.NextFrame(() =>
                {
                    if (!knife.IsValid)
                        return;

                    knife.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
                    knife.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
                    knife.AttributeManager.Item.ItemDefinitionIndex = defIndex;
                    knife.AttributeManager.Item.EntityQuality = 3;
                    UpdateItemId(knife.AttributeManager.Item);

                    if (paintKit > 0)
                        WriteAttributes(knife.AttributeManager.Item, paintKit, wear, seed);

                    knife.FallbackPaintKit = paintKit;
                    knife.FallbackWear = wear;
                    knife.FallbackSeed = seed;

                    Utilities.SetStateChanged(knife, "CEconEntity", "m_AttributeManager");
                    Utilities.SetStateChanged(
                        player,
                        "CCSPlayerController",
                        "m_pInventoryServices"
                    );

                    Server.NextFrame(() =>
                    {
                        if (!player.IsValid || !player.PawnIsAlive)
                            return;
                        Server.NextFrame(() =>
                        {
                            if (!player.IsValid || !player.PawnIsAlive)
                                return;
                            player.ExecuteClientCommand("slot1");
                        });
                    });
                });
            },
            TimerFlags.STOP_ON_MAPCHANGE
        );
    }
    #endregion


    private void RefreshActiveWeapon(
        CCSPlayerController player,
        CBasePlayerWeapon weapon,
        int paintKit,
        float wear,
        int seed,
        bool legacy
    )
    {
        ApplySkin(player, weapon, paintKit, wear, seed, legacy);

        ushort defIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
        string giveName = ResolveGiveName(defIndex, weapon.DesignerName);
        int clip1 = weapon.Clip1;
        int reserve = weapon.ReserveAmmo[0];

        void FinishGive(CBasePlayerWeapon w)
        {
            if (w == null || !w.IsValid)
                return;
            w.Clip1 = clip1;
            w.ReserveAmmo[0] = reserve;
            ApplySkin(player, w, paintKit, wear, seed, legacy);
        }

        weapon.AddEntityIOEvent("Kill", weapon, null, "", 0.1f);

        plugin.AddTimer(
            0.25f,
            () =>
            {
                if (!player.IsValid || !player.PawnIsAlive)
                    return;

                var handle = player.GiveNamedItem(giveName);
                if (handle == IntPtr.Zero)
                    return;

                var newWeapon = new CBasePlayerWeapon(handle);

                Server.NextFrame(() =>
                {
                    if (newWeapon == null || !newWeapon.IsValid)
                        return;

                    if (newWeapon.AttributeManager.Item.ItemDefinitionIndex != defIndex)
                    {
                        newWeapon.AcceptInput("ChangeSubclass", value: defIndex.ToString());
                        newWeapon.AttributeManager.Item.ItemDefinitionIndex = defIndex;
                        var settled = newWeapon;
                        Server.NextFrame(() => FinishGive(settled));
                        return;
                    }

                    FinishGive(newWeapon);
                });
            },
            TimerFlags.STOP_ON_MAPCHANGE
        );
    }

    private void ApplySkin(
        CCSPlayerController player,
        CBasePlayerWeapon weapon,
        int paintKit,
        float wear,
        int seed,
        bool legacy
    )
    {
        weapon.AttributeManager.Item.EntityQuality = 0;
        weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
        weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();

        var id = _nextItemId++;
        weapon.AttributeManager.Item.ItemID = id;
        weapon.AttributeManager.Item.ItemIDLow = (uint)(id & 0xFFFFFFFF);
        weapon.AttributeManager.Item.ItemIDHigh = (uint)(id >> 32);

        weapon.AttributeManager.Item.AccountID = (uint)player.SteamID;

        AttributeSet.Invoke(
            weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
            "set item texture prefab",
            paintKit
        );

        weapon.FallbackPaintKit = paintKit;
        weapon.FallbackWear = wear;
        weapon.FallbackSeed = seed;
        weapon.FallbackStatTrak = -1;

        if (weapon.CBodyComponent?.SceneNode != null)
            weapon.AcceptInput("SetBodygroup", value: $"body,{(legacy ? 1 : 0)}");
    }

    #region Knife and Glove Utilities
    private static void SetBodygroup(CCSPlayerPawn pawn, string group, int value)
    {
        if (pawn == null || !pawn.IsValid)
            return;
        pawn.AcceptInput("SetBodygroup", value: $"{group},{value}");
    }

    private static void WriteAttributes(CEconItemView item, int paintKit, float wear, int seed)
    {
        foreach (
            var handle in new[]
            {
                item.NetworkedDynamicAttributes.Handle,
                item.AttributeList.Handle,
            }
        )
        {
            AttributeSet.Invoke(handle, "set item texture prefab", paintKit);
            AttributeSet.Invoke(handle, "set item texture wear", wear);
            AttributeSet.Invoke(handle, "set item texture seed", seed);
        }
    }

    private static void UpdateItemId(CEconItemView item)
    {
        var id = _nextItemId++;
        item.ItemID = id;
        item.ItemIDLow = (uint)(id & 0xFFFFFFFF);
        item.ItemIDHigh = (uint)(id >> 32);
    }

    private static CsTeam ParseTeam(string value)
    {
        string trimmed = value.Trim();
        if (trimmed == "ct")
            return CsTeam.CounterTerrorist;
        if (trimmed == "t")
            return CsTeam.Terrorist;
        return CsTeam.None;
    }
    #endregion
}
