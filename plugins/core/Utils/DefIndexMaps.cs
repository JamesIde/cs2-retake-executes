namespace RetakeExecutesPlugin;

public static class DefIndexMap
{
    public static readonly Dictionary<string, ushort> KnifeDefIndex = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        { "bayonet", 500 },
        { "classic", 503 },
        { "flip", 505 },
        { "gut", 506 },
        { "karambit", 507 },
        { "m9", 508 },
        { "huntsman", 509 },
        { "falchion", 512 },
        { "bowie", 514 },
        { "butterfly", 515 },
        { "shadow", 516 },
        { "paracord", 517 },
        { "survival", 518 },
        { "ursus", 519 },
        { "navaja", 520 },
        { "nomad", 521 },
        { "stiletto", 522 },
        { "talon", 523 },
        { "skeleton", 525 },
        { "kukri", 526 },
    };

    // These are set directly on pawn.EconGloves.ItemDefinitionIndex — no entity
    // spawning required. Defindexes from Nereziel/cs2-WeaponPaints gloves data.
    public static readonly Dictionary<string, ushort> GloveDefIndex = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        { "bloodhound", 5027 },
        { "brokenfang", 4725 },
        { "driver", 5031 },
        { "handwraps", 5032 },
        { "moto", 5033 },
        { "specialist", 5034 },
        { "sport", 5030 },
        { "hydra", 5035 },
    };

    public static readonly Dictionary<string, ushort> WeaponDefIndex = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        { "weapon_ak47", 7 },
        { "weapon_aug", 8 },
        { "weapon_awp", 9 },
        { "weapon_famas", 10 },
        { "weapon_g3sg1", 11 },
        { "weapon_galilar", 13 },
        { "weapon_m249", 14 },
        { "weapon_m4a1", 16 },
        { "weapon_mac10", 17 },
        { "weapon_p90", 19 },
        { "weapon_mp5sd", 23 },
        { "weapon_ump45", 24 },
        { "weapon_xm1014", 25 },
        { "weapon_bizon", 26 },
        { "weapon_mag7", 27 },
        { "weapon_negev", 28 },
        { "weapon_sawedoff", 29 },
        { "weapon_tec9", 30 },
        { "weapon_zeus", 31 },
        { "weapon_p2000", 32 },
        { "weapon_mp7", 33 },
        { "weapon_mp9", 34 },
        { "weapon_nova", 35 },
        { "weapon_p250", 36 },
        { "weapon_scar20", 38 },
        { "weapon_sg556", 39 },
        { "weapon_ssg08", 40 },
        { "weapon_m4a1_silencer", 60 },
        { "weapon_usp_silencer", 61 },
        { "weapon_cz75a", 63 },
        { "weapon_revolver", 64 },
        { "weapon_deagle", 1 },
        { "weapon_elite", 2 },
        { "weapon_fiveseven", 3 },
        { "weapon_glock", 4 },
    };

    public static readonly Dictionary<ushort, string> DefIndexToKnife = KnifeDefIndex.ToDictionary(
        kv => kv.Value,
        kv => kv.Key
    );

    public static readonly Dictionary<ushort, string> DefIndexToWeapon =
        WeaponDefIndex.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static readonly Dictionary<ushort, string> DefIndexToGlove = GloveDefIndex.ToDictionary(
        kv => kv.Value,
        kv => kv.Key
    );
}
