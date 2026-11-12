namespace PortfolioManager.MathModule.Client;

/// <summary>Yahoo Finance knows no such ticker (the math service answered 404 symbol_not_found). Returned as 400.</summary>
public class SymbolNotFoundException : Exception
{
    public const string DefaultMessage = "Такий тікер не знайдено";

    public SymbolNotFoundException() : base(DefaultMessage) { }

    public SymbolNotFoundException(string message) : base(message) { }

    public SymbolNotFoundException(string message, Exception inner) : base(message, inner) { }
}
