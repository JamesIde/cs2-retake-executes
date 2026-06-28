using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class PlayerDataService(IDatabaseService _databaseService)
{
    public readonly ConcurrentDictionary<ulong, PlayerData> playerData = new();
    private readonly IDatabaseService databaseService = _databaseService;

    public async Task RetrievePlayerData(ulong steamId)
    {
        Log.Info($"Retrieve player data service called for {steamId}");

        var existingUser = await databaseService.RetrieveUser(steamId);

        if (existingUser is null)
        {
            Log.Info($"No user found for {steamId}, creating...");
            var newUser = await databaseService.CreateUser(steamId);
            Log.Info($"Created user {steamId}");
            await databaseService.SeedPreferences(steamId);
            var preferences = await databaseService.RetrievePreferences(steamId);
            playerData.TryAdd(newUser.SteamId, preferences);
            return;
        }

        var existingUserPreferences = await databaseService.RetrievePreferences(steamId);
        playerData.TryAdd(existingUser.SteamId, existingUserPreferences);
    }

    public async Task ReloadPlayerData(ulong steamId)
    {
        Log.Info($"Reloading player data for player {steamId}");
        var preferences = await databaseService.RetrievePreferences(steamId);
        playerData[steamId] = preferences;
        Log.Info($"Reloaded player data for {steamId}");
    }

    public async Task RetrievePlayerSkins(ulong steamId)
    {
        var skins = await databaseService.RetrieveSkins(steamId);
        // PrintPlayerSkins(skins);
        playerData[steamId].PlayerSkinPreferences = skins;
    }

    public void RemovePlayerData(ulong steamId)
    {
        playerData.Remove(steamId, out _);
    }

    public List<ulong> RetrievePlayersWantingAwp(CsTeam team, IEnumerable<ulong> teamPlayerIds)
    {
        List<ulong> playersWantingAwp = [];

        if (team == CsTeam.CounterTerrorist)
        {
            foreach (var (key, value) in playerData.Where(p => teamPlayerIds.Contains(p.Key)))
            {
                if (value.CTPreferences.WantsAwp)
                    playersWantingAwp.Add(key);
            }
        }

        if (team == CsTeam.Terrorist)
        {
            foreach (var (key, value) in playerData.Where(p => teamPlayerIds.Contains(p.Key)))
            {
                if (value.TPreferences.WantsAwp)
                    playersWantingAwp.Add(key);
            }
        }

        return playersWantingAwp;
    }

    public async Task RecordPlayerEvent(ulong steamId, PlayerEventType eventType)
    {
        var eventTypeString = MapEventToString(eventType);
        await databaseService.RecordPlayerEvent(steamId, eventTypeString);
    }

    public async Task RecordEventLog(RoundEventLog eventLog)
    {
        await databaseService.RecordRoundEvent(eventLog);
    }

    public async Task BanPlayer(ulong steamId, PlayerBan ban)
    {
        await databaseService.CreatePlayerBan(steamId, ban);
    }

    public async Task<List<PlayerBan>> RetrievePlayerBans(ulong steamId)
    {
        return await databaseService.RetrievePlayerBans(steamId);
    }

    public void PrintPlayerData()
    {
        if (playerData.IsEmpty)
        {
            Log.Info("Player data cache is empty");
            return;
        }

        Log.Info($"Player data cache ({playerData.Count} players):");
        Log.Info(new string('-', 40));

        foreach (var (steamId, data) in playerData)
        {
            Log.Info($"SteamID: {steamId}");
            Log.Info($"  CT Preferences:");
            Log.Info($"    Force Buy:    {data.CTPreferences.ForceBuyWeapon}");
            Log.Info($"    Full Buy:     {data.CTPreferences.FullBuyWeapon}");
            Log.Info($"    Pistol Round: {data.CTPreferences.PistolRoundWeapon}");
            Log.Info($"    Wants AWP:    {data.CTPreferences.WantsAwp}");
            Log.Info($"  T Preferences:");
            Log.Info($"    Force Buy:    {data.TPreferences.ForceBuyWeapon}");
            Log.Info($"    Full Buy:     {data.TPreferences.FullBuyWeapon}");
            Log.Info($"    Pistol Round: {data.TPreferences.PistolRoundWeapon}");
            Log.Info($"    Wants AWP:    {data.TPreferences.WantsAwp}");
            Log.Info(new string('-', 40));
        }
    }

    private void PrintPlayerSkins(List<PlayerSkin> skins)
    {
        Log.Info($"Player skins for player ");
        Log.Info(new string('-', 40));
        foreach (var skin in skins)
        {
            string type =
                skin.IsGlove ? "Glove"
                : skin.IsKnife ? "Knife"
                : skin.IsWeapon ? "Weapon"
                : "Unknown";

            Log.Info($"[PlayerSkin] Team: {skin.Team} | Seed: {skin.Seed} | Type: {type}");
            Log.Info(
                $"  Weapon Index: {skin.WeaponIndex} | Paint Index: {skin.WeaponPaintIndex} | Float: {skin.WeaponFloat:F4}"
            );
        }
        Log.Info(new string('-', 40));
    }

    private static string MapEventToString(PlayerEventType eventType)
    {
        if (eventType == PlayerEventType.ServerConnect)
            return "server_connect";
        if (eventType == PlayerEventType.ServerDisconnect)
            return "server_disconnect";
        return string.Empty;
    }
}
