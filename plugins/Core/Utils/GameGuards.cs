namespace RetakeExecutesPlugin;

public static class GameGuards
{
    public static bool IsAdmin(string? steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return false;
        return Constants.ADMIN_STEAM_ID.Contains(steamId);
    }
}
