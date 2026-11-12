namespace PortfolioManager.Common.Validation;

/// <summary>
/// What a ticker may look like when it comes from the client: 1 to 10 characters (the database column is 10 wide),
/// starting with a letter or a digit, then letters, digits, a dot or a hyphen: AAPL, BRK-B, BRK.B, 0700.HK.
/// Whether the ticker really exists is decided by the market data service; this only keeps garbage out.
/// </summary>
public static class SymbolFormat
{
    public const int MaxLength = 10;
    // \z, not $: a $ would also accept a line break at the end.
    public const string Pattern = @"^[A-Za-z0-9][A-Za-z0-9.\-]{0,9}\z";
    public const string ErrorMessage = "Тікер має містити від 1 до 10 символів: літери, цифри, крапку або дефіс.";
}
