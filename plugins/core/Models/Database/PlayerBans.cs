namespace RetakeExecutesPlugin;

public class DbPlayerBan
{
#pragma warning disable IDE1006 // Naming Styles
    public required Guid id { get; set; }
    public required ulong steam_id { get; set; }
    public required DateTime ban_expires_at { get; set; }
}

public class PlayerBan
{
    public required ulong SteamId { get; set; }
    public required DateTime BanExpiresAt { get; set; }
}

public static class PlayerBanMapper
{
    public static PlayerBan ToDomain(DbPlayerBan dto)
    {
        return new PlayerBan { BanExpiresAt = dto.ban_expires_at, SteamId = dto.steam_id };
    }

    public static List<PlayerBan> ToDomainList(List<DbPlayerBan> dto)
    {
        if (dto.Count == 0)
            return [];
        return
        [
            .. dto.Select(ban => new PlayerBan
            {
                BanExpiresAt = ban.ban_expires_at,
                SteamId = ban.steam_id,
            }),
        ];
    }
}
