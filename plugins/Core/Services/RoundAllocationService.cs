namespace RetakeExecutesPlugin;

/**
Another approach to do after the weapon allocation service is done:
When the map loads, fill out an array of X elements with X amount of round types,
Shuffle them, put them into a queue to be popped off after each round ends.

I.e. 21 rounds, I want 10 full buys, 5 pistol rounds, 3 force buys, 3 half buys.
But, I don't care about what order they occur. So we can 'preload' the round types on map load.
I think thats better than dealing with this weighting nonsense. But it can wait.
**/

public class RoundAllocationService(GameState _gameState)
{
    private readonly GameState gameState = _gameState;
    private static readonly Random random = new();

    #region Allocation Weighting

    private static readonly (double min, double max) PistolRoundWeighting = (0.0, 0.2);
    private static readonly (double min, double max) FullRoundWeighting = (0.2, 0.7);
    private static readonly (double min, double max) ForceBuyWeighting = (0.7, 1);
    #endregion

    /// <summary>
    /// Sets the round type.
    /// Possible rounds: Pistol, Force Buy, Full Buy
    /// </summary>
    public void SetRoundType()
    {
        var roundType = DetermineRoundType();
        gameState.SetCurrentRoundType(roundType);
    }

    private static RoundType DetermineRoundType()
    {
        var value = random.NextDouble();

        var result = value switch
        {
            _ when Between(value, PistolRoundWeighting) => RoundType.PistolRound,
            _ when Between(value, FullRoundWeighting) => RoundType.FullBuy,
            _ when Between(value, ForceBuyWeighting) => RoundType.ForceBuy,
            _ => RoundType.FullBuy,
        };
        return result;
    }

    private static bool Between(double input, (double min, double max) condition)
    {
        return input >= condition.min && input < condition.max;
    }
}
