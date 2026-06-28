using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class PlayerState
{
    public CsTeam Team { get; set; }
    public required PlayerStatus Status { get; set; }
    public required DateTime DateConnected { get; set; }
    public required string PlayerName { get; set; }
}

public class PlayerData
{
    public required WeaponPreferences CTPreferences { get; set; }
    public required WeaponPreferences TPreferences { get; set; }
    public List<PlayerSkin> PlayerSkinPreferences { get; set; } = [];
}
