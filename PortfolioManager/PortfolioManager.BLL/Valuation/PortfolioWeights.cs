namespace PortfolioManager.BLL.Valuation;

/// <summary>The share of each ticker in a portfolio, from what its positions are worth now.</summary>
public static class PortfolioWeights
{
    /// <summary>
    /// One entry per ticker, in the order the tickers first appear. A ticker held in several positions is
    /// added up, and its name is upper-cased because the market data service does the same. Positions worth
    /// nothing are left out. The weights add up to 1, or the result is empty when nothing is worth anything.
    /// </summary>
    public static (IReadOnlyList<string> Symbols, IReadOnlyList<double> Weights) FromValues(
        IEnumerable<(string Symbol, double Value)> positions)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (symbol, value) in positions)
        {
            if (!(value > 0) || !double.IsFinite(value))
                continue;

            var key = symbol.Trim().ToUpperInvariant();
            values[key] = values.GetValueOrDefault(key) + value;
        }

        var total = values.Values.Sum();
        return total > 0
            ? (values.Keys.ToList(), values.Values.Select(value => value / total).ToList())
            : (Array.Empty<string>(), Array.Empty<double>());
    }
}
