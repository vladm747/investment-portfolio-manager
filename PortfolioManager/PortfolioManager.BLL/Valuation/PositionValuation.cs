namespace PortfolioManager.BLL.Valuation;

/// <summary>
/// What a holding is worth and what it has earned. The only place in the code that calculates these numbers:
/// reading a position, a portfolio and its totals all come here, so they cannot disagree. Nothing derived is
/// stored. Pure: no database, no clock.
/// </summary>
/// <param name="EntryValue">What was paid: quantity times entry price.</param>
/// <param name="CurrentValue">What it is worth now: quantity times current price.</param>
/// <param name="Gain">Current value minus entry value.</param>
/// <param name="GainPercentage">Gain as a percentage of the entry value, to four decimals; 0 when nothing was paid.</param>
public readonly record struct PositionValuation(decimal EntryValue, decimal CurrentValue, decimal Gain, decimal GainPercentage)
{
    public static PositionValuation Of(int quantity, decimal entryPrice, decimal currentPrice) =>
        FromValues(quantity * entryPrice, quantity * currentPrice);

    /// <summary>The valuation of a whole portfolio: the sums over its positions, with the percentage worked out again.</summary>
    public static PositionValuation Sum(IEnumerable<PositionValuation> positions)
    {
        decimal entryValue = 0, currentValue = 0;
        foreach (var position in positions)
        {
            entryValue += position.EntryValue;
            currentValue += position.CurrentValue;
        }

        return FromValues(entryValue, currentValue);
    }

    private static PositionValuation FromValues(decimal entryValue, decimal currentValue)
    {
        var gain = currentValue - entryValue;

        // Nothing paid means no percentage (and no division by zero).
        var percentage = entryValue > 0 ? Math.Round(gain / entryValue * 100, 4, MidpointRounding.AwayFromZero) : 0;

        return new PositionValuation(entryValue, currentValue, gain, percentage);
    }
}
