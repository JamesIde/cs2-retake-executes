using System.Globalization;

namespace RetakeExecutesPlugin;

public class PlayerSkin
{
    public required string Team { get; set; }
    public required int Seed { get; set; }
    public required ushort WeaponIndex { get; set; }
    public required int WeaponPaintIndex { get; set; }
    public required float WeaponFloat { get; set; }
    public bool IsGlove { get; set; }
    public bool IsKnife { get; set; }
    public bool IsWeapon { get; set; }
    public bool IsLegacyModel { get; set; }
    public required string WeaponName { get; set; }
}

public class DbPlayerSkin
{
#pragma warning disable IDE1006 // Naming Styles
    public required Guid id { get; set; }
    public required string steam_id { get; set; }
    public required string team { get; set; }
    public int seed { get; set; }
    public required string floatNo { get; set; }
    public required string weapon_paint_index { get; set; }
    public int weapon_defindex { get; set; }
    public bool isGlove { get; set; }
    public bool isKnife { get; set; }
    public bool isWeapon { get; set; }
    public bool legacy_model { get; set; }
    public required string weapon_name { get; set; }
}

public enum Team
{
    ct,
    t,
}

public static class PlayerSkinMapper
{
    public static List<PlayerSkin> ToDomain(List<DbPlayerSkin>? dto)
    {
        if (dto.Count == 0)
            return [];

        return
        [
            .. dto.Select(d => new PlayerSkin
            {
                Team = d.team,
                Seed = d.seed,
                WeaponIndex = (ushort)d.weapon_defindex,
                WeaponFloat = ParseFloat(d.floatNo),
                WeaponPaintIndex = int.Parse(d.weapon_paint_index),
                IsGlove = d.isGlove,
                IsKnife = d.isKnife,
                IsWeapon = d.isWeapon,
                IsLegacyModel = d.legacy_model,
                WeaponName = d.weapon_name,
            }),
        ];
    }

    private static float ParseFloat(string value)
    {
        if (!float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatNo))
        {
            return 0.01f;
        }
        return floatNo;
    }
}
