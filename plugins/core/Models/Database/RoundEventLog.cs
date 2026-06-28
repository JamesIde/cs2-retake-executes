namespace RetakeExecutesPlugin;

public class DbRoundEventLog
{
#pragma warning disable IDE1006 // Naming Styles

    public string id { get; set; } = string.Empty;
    public string map_name { get; set; } = string.Empty;
    public string round_type { get; set; } = string.Empty;
    public string strategy_type { get; set; } = string.Empty;
    public string winning_side { get; set; } = string.Empty;
    public bool bombPlanted { get; set; }
    public int remaining_ct { get; set; }
    public int remaining_t { get; set; }
    public int total_player_count { get; set; }
    public DateTime? date_generated { get; set; }
}

public enum WinningSide
{
    Ct,
    T,
}

public class RoundEventLog
{
    public required string MapName { get; set; }
    public required string RoundType { get; set; }
    public required string StrategyType { get; set; }
    public required string WinningSide { get; set; }
    public bool BombPlanted { get; set; }
    public int RemainingCt { get; set; }
    public int RemainingT { get; set; }
    public int TotalPlayerCount { get; set; }
}
