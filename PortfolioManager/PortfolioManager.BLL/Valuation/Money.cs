namespace PortfolioManager.BLL.Valuation;

/// <summary>
/// Money is <c>decimal</c> with four decimals, the same as the database columns, rounded the way SQL Server rounds
/// when it stores a value (half away from zero). Rounding here first means the number a request returns is the
/// number a later request will read back.
/// </summary>
public static class Money
{
    public const int Scale = 4;

    public static decimal Round(decimal amount) =>
        Math.Round(amount, Scale, MidpointRounding.AwayFromZero);

    /// <summary>
    /// A price from the market data service, which sends a binary float such as 189.84000396728516. The conversion
    /// to decimal keeps only the digits a float really has, and rounding to four decimals drops the float's noise.
    /// </summary>
    public static decimal FromMarketPrice(double price) => Round((decimal)price);
}
