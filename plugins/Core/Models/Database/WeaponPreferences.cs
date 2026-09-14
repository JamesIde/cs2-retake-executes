using System.Reflection;
using System.Runtime.Serialization;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakeExecutesPlugin;

public class WeaponPreferences
{
    public required CsItem ForceBuyWeapon { get; set; }
    public required CsItem FullBuyWeapon { get; set; }
    public required CsItem PistolRoundWeapon { get; set; }
    public required bool WantsAwp { get; set; }
}

public class DbWeaponPreferences
{
#pragma warning disable IDE1006 // Naming Styles
    public required string force_buy_weapon { get; set; }
    public required string full_buy_weapon { get; set; }
    public required string pistol_round_weapon { get; set; }
    public required bool wants_awp { get; set; }
}

public static class WeaponPreferenceMapper
{
    public static WeaponPreferences ToDomain(DbWeaponPreferences dto)
    {
        return new WeaponPreferences
        {
            ForceBuyWeapon = ParseWeaponString(dto.force_buy_weapon),
            FullBuyWeapon = ParseWeaponString(dto.full_buy_weapon),
            PistolRoundWeapon = ParseWeaponString(dto.pistol_round_weapon),
            WantsAwp = dto.wants_awp,
        };
    }

    public static CsItem ParseWeaponString(string weaponString)
    {
        foreach (var field in typeof(CsItem).GetFields())
        {
            var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute?.Value == weaponString)
                return (CsItem)field.GetValue(null)!;
        }
        throw new ArgumentException($"No CsItem found for weapon string: {weaponString}");
    }

    public static string ToWeaponString(this CsItem item)
    {
        var field = typeof(CsItem).GetField(item.ToString());
        var attribute = field?.GetCustomAttribute<EnumMemberAttribute>();
        return attribute?.Value ?? item.ToString();
    }
}
