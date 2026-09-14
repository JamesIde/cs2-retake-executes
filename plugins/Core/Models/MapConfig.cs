using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public record SelectedRound
{
    public required Round Round { get; set; }
}

public class MapConfiguration
{
    public string? CreatedAt { get; set; }
    public string? ConfigVersion { get; set; }
    public string? LastUpdated { get; set; }
    public string? LastUpdatedBy { get; set; }
    public List<Round> MapRounds { get; set; } = [];

    public int GetSmokeCount() => MapRounds?.FirstOrDefault()?.Smokes.Count ?? 0;

    public int GetCTSpawnCount() =>
        MapRounds?.FirstOrDefault()?.Spawns.CounterTerroristSpawns.Count ?? 0;

    public int GetTSpawnCount() => MapRounds?.FirstOrDefault()?.Spawns.TerroristSpawns.Count ?? 0;

    public int GetRoundCount() => MapRounds.Count;

    public Round? GetRoundById(int roundId)
    {
        if (MapRounds.Count == 0)
            throw new Exception("Map config round count is 0");

        return MapRounds.FirstOrDefault(r => r.RoundIdentifier == roundId);
    }

    /// <summary>
    /// Resets all fields to their default state. Call this on map change before loading new config.
    /// </summary>
    public void ResetConfig()
    {
        CreatedAt = null;
        ConfigVersion = null;
        LastUpdated = null;
        LastUpdatedBy = null;
        MapRounds = [];
    }

    /// <summary>
    /// Returns a new empty MapConfiguration.
    /// </summary>
    public static MapConfiguration Empty() => new();
}

public record Round
{
    public required int RoundIdentifier { get; set; }
    public required string RoundStrategy { get; set; }
    public required List<Projectile> Smokes { get; set; } = [];
    public required List<Projectile> Flashes { get; set; } = [];
    public required Spawns Spawns { get; set; }
}

public record Spawns
{
    public required List<SpawnData> CounterTerroristSpawns { get; set; } = [];
    public required List<SpawnData> TerroristSpawns { get; set; } = [];
}

public record Projectile(string Name, Vector Origin, Vector Velocity, QAngle Angle, float Delay);

public record SpawnData(string Label, CsTeam Team, Vector Origin, QAngle Angle, int SpawnId);
