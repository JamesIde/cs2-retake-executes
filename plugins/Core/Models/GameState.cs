using System.Threading.Tasks.Dataflow;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class GameState
{
    public bool IsPracMode { get; set; } = false;
    public bool HasMatchStarted { get; set; } = false;
    public bool IsRoundLive { get; set; } = false;
    public bool CanScrambleTeams { get; set; } = false;

    public RoundType? CurrentRoundType { get; set; }
    public Round? CurrentRoundStrategy { get; set; }

    public ulong? SelectedTAwper { get; set; }
    public ulong? SelectedCTAwper { get; set; }
    public ulong? SelectedBombCarrier { get; set; }

    public CsTeam LastRoundWin { get; set; } = CsTeam.None;

    public int CTConsecutiveWins { get; set; } = 0;
    public int TConsecutiveWins { get; set; } = 0;
    public int RoundsPlayed { get; set; } = 0;

    public string RtvMidGameNextMap { get; set; } = string.Empty;
    public string EndGameNextMap { get; set; } = string.Empty;

    // Round Type state
    public void SetCurrentRoundType(RoundType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        CurrentRoundType = type;
    }

    public RoundType GetCurrentRoundType()
    {
        if (CurrentRoundType is null)
        {
            throw new InvalidOperationException("No round is active.");
        }
        // Round type needs to be null. As it needs to be reset on map end.. why we need the explicit cast i dont know.
        return (RoundType)(CurrentRoundType is not null ? CurrentRoundType : RoundType.FullBuy);
    }

    public string? GetCurrentRoundTypeForAnnouncement()
    {
        if (CurrentRoundType is null)
        {
            throw new InvalidOperationException("No round is active.");
        }
        return GameHelpers.MapRoundTypeToString(CurrentRoundType);
    }

    public void SetCurrentRoundStrategy(Round round)
    {
        CurrentRoundStrategy = round;
    }

    public Round GetCurrentRoundStrategy()
    {
        ArgumentNullException.ThrowIfNull(CurrentRoundStrategy);
        return CurrentRoundStrategy;
    }

    public void ResetGameState()
    {
        CurrentRoundStrategy = null;
        CurrentRoundType = null;
        SelectedCTAwper = null;
        SelectedTAwper = null;
        SelectedBombCarrier = null;
        IsRoundLive = false;
        HasMatchStarted = false;
        RoundsPlayed = 0;
        RtvMidGameNextMap = string.Empty;
        EndGameNextMap = string.Empty;
    }

    public void ResetStreakThreshold()
    {
        TConsecutiveWins = 0;
        CTConsecutiveWins = 0;
        LastRoundWin = CsTeam.None;
    }
}
