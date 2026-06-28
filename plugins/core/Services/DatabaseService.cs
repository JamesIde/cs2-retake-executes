using CounterStrikeSharp.API.Modules.Entities.Constants;
using Dapper;
using MySqlConnector;

namespace RetakeExecutesPlugin;

public interface IDatabaseService
{
    Task<SteamUser?> RetrieveUser(ulong steamId);
    Task<SteamUser> CreateUser(ulong steamId);
    Task SeedPreferences(ulong steamId);
    Task<PlayerData> RetrievePreferences(ulong steamId);
    Task<List<PlayerSkin>> RetrieveSkins(ulong steamId);
    Task RecordPlayerEvent(ulong steamId, string eventType);
    Task RecordRoundEvent(RoundEventLog eventLog);

    Task CreatePlayerBan(ulong steamId, PlayerBan ban);
    Task<List<PlayerBan>> RetrievePlayerBans(ulong steamId);
}

public class DatabaseService(string _connectionString, string _schema) : IDatabaseService
{
    private readonly string connectionString = _connectionString;
    private readonly string schema = _schema;

    public async Task<SteamUser?> RetrieveUser(ulong steamId)
    {
        using var conn = new MySqlConnection(connectionString);
        var db = await conn.QueryFirstOrDefaultAsync<DbSteamUser>(
            $"SELECT * FROM {schema}.steam_users WHERE steam_id = @SteamId",
            new { SteamId = steamId }
        );
        return db == null ? null : SteamUserMapper.ToDomain(db);
    }

    public async Task<SteamUser> CreateUser(ulong steamId)
    {
        var newUser = new SteamUser { CreatedAt = DateTime.UtcNow, SteamId = steamId };
        using var conn = new MySqlConnection(connectionString);
        var db = await conn.ExecuteAsync(
            $"INSERT INTO {schema}.steam_users (steam_id, created_at) VALUES (@SteamId, @CreatedAt)",
            newUser
        );

        return newUser;
    }

    public async Task SeedPreferences(ulong steamId)
    {
        using var conn = new MySqlConnection(connectionString);

        await conn.ExecuteAsync(
            @"
         INSERT INTO ct_weapon_preferences (id, steam_id, force_buy_weapon, full_buy_weapon, pistol_round_weapon, wants_awp, updated_at)
        VALUES (@Id, @SteamId, @ForceBuy, @FullBuy, @PistolRound, @WantsAwp, @UpdatedAt)
        ON DUPLICATE KEY UPDATE steam_id = steam_id",
            new
            {
                Id = Guid.NewGuid(),
                SteamId = steamId,
                ForceBuy = CsItem.FiveSeven.ToWeaponString(),
                FullBuy = CsItem.M4A1S.ToWeaponString(),
                PistolRound = CsItem.USP.ToWeaponString(),
                WantsAwp = true,
                UpdatedAt = DateTime.UtcNow,
            }
        );

        await conn.ExecuteAsync(
            @"
        INSERT INTO t_weapon_preferences (id, steam_id, force_buy_weapon, full_buy_weapon, pistol_round_weapon, wants_awp, updated_at)
        VALUES (@Id, @SteamId, @ForceBuy, @FullBuy, @PistolRound, @WantsAwp, @UpdatedAt)
        ON DUPLICATE KEY UPDATE steam_id = steam_id",
            new
            {
                Id = Guid.NewGuid(),
                SteamId = steamId,
                ForceBuy = CsItem.Tec9.ToWeaponString(),
                FullBuy = CsItem.AK47.ToWeaponString(),
                PistolRound = "weapon_glock", // WTF - CsItem.Glock just results to Glock18 ~! and not weapon_glock
                WantsAwp = true,
                UpdatedAt = DateTime.UtcNow,
            }
        );
    }

    public async Task<PlayerData> RetrievePreferences(ulong steamId)
    {
        using var conn = new MySqlConnection(connectionString);

        var dbCt = await conn.QueryFirstOrDefaultAsync<DbWeaponPreferences>(
            $"SELECT force_buy_weapon, full_buy_weapon, pistol_round_weapon, wants_awp FROM {schema}.ct_weapon_preferences WHERE steam_id = @SteamId",
            new { SteamId = steamId }
        );

        var dbT = await conn.QueryFirstOrDefaultAsync<DbWeaponPreferences>(
            $"SELECT force_buy_weapon, full_buy_weapon, pistol_round_weapon, wants_awp FROM {schema}.t_weapon_preferences WHERE steam_id = @SteamId",
            new { SteamId = steamId }
        );

        if (dbCt is null || dbT is null)
        {
            throw new Exception("One or both preference tables are empty.");
        }

        var playerData = new PlayerData
        {
            CTPreferences = WeaponPreferenceMapper.ToDomain(dbCt),
            TPreferences = WeaponPreferenceMapper.ToDomain(dbT),
        };

        return playerData;
    }

    public async Task<List<PlayerSkin>> RetrieveSkins(ulong steamId)
    {
        using var conn = new MySqlConnection(connectionString);

        var query =
            $@"
            SELECT ps.*,
            CASE WHEN ps.isWeapon = 1 THEN ws.legacy_model END AS legacy_model,
            CASE WHEN ps.isWeapon = 1 THEN ws.weapon_paint END as weapon_name
            FROM {schema}.player_skins ps
            LEFT JOIN {schema}.weapon_skins ws
            ON ws.weapon_defindex = ps.weapon_defindex
            AND ws.weapon_paint_index = ps.weapon_paint_index
            WHERE ps.steam_id = @SteamId;
        ";

        var db = (await conn.QueryAsync<DbPlayerSkin>(query, new { SteamId = steamId })).ToList();

        return PlayerSkinMapper.ToDomain(db);
    }

    public async Task RecordPlayerEvent(ulong steamId, string eventType)
    {
        using var conn = new MySqlConnection(connectionString);

        await conn.ExecuteAsync(
            "INSERT INTO player_event_log (id, steam_id, event_type) VALUES (@Id, @SteamId, @EventType)",
            new
            {
                Id = Guid.NewGuid().ToString(),
                SteamId = steamId,
                EventType = eventType,
            }
        );
    }

    public async Task RecordRoundEvent(RoundEventLog eventLog)
    {
        using var conn = new MySqlConnection(connectionString);

        var query =
            @"
        INSERT INTO round_event_log
            (id, map_name, round_type, strategy_type, winning_side,
             bomb_planted, remaining_ct, remaining_t, total_player_count)
        VALUES
            (@Id, @MapName, @RoundType, @StrategyType, @WinningSide,
             @BombPlanted, @RemainingCt, @RemainingT, @TotalPlayerCount);";

        var parameters = new
        {
            Id = Guid.NewGuid().ToString(),
            eventLog.MapName,
            eventLog.RoundType,
            eventLog.StrategyType,
            eventLog.WinningSide,
            eventLog.BombPlanted,
            eventLog.RemainingCt,
            eventLog.RemainingT,
            eventLog.TotalPlayerCount,
        };

        await conn.ExecuteAsync(query, parameters);
    }

    public async Task CreatePlayerBan(ulong steamId, PlayerBan ban)
    {
        using var conn = new MySqlConnection(connectionString);

        var query =
            @"
        INSERT INTO player_bans
            (id, steam_id, date_generated, ban_expires_at)
        VALUES (@Id, @SteamId, @DateGenerated, @BanExpiresAt)
        ";

        var parameters = new
        {
            Id = Guid.NewGuid().ToString(),
            ban.SteamId,
            DateGenerated = DateTime.UtcNow,
            ban.BanExpiresAt,
        };

        await conn.ExecuteAsync(query, parameters);
    }

    public async Task<List<PlayerBan>> RetrievePlayerBans(ulong steamId)
    {
        using var conn = new MySqlConnection(connectionString);

        var db = await conn.QueryAsync<DbPlayerBan>(
            $"SELECT * from {schema}.player_bans WHERE steam_id = @SteamId",
            new { SteamId = steamId }
        );
        return PlayerBanMapper.ToDomainList([.. db]);
    }
}
