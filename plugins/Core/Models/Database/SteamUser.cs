namespace RetakeExecutesPlugin;

public class SteamUser
{
    public required ulong SteamId { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class DbSteamUser
{
#pragma warning disable IDE1006 // Naming Styles
    public ulong steam_id { get; set; }
    public DateTime created_at { get; set; }
}

public static class SteamUserMapper
{
    public static SteamUser ToDomain(DbSteamUser dto)
    {
        return new SteamUser { CreatedAt = dto.created_at, SteamId = dto.steam_id };
    }
}
