namespace PortfolioManager.MathModule.Client;

/// <summary>
/// The math service could not be reached or could not answer: connection refused, timeout, 5xx after the
/// retries, a wrong API key, or a response that does not match the contract. Returned as 502.
/// The details are in the log, not in the message.
/// </summary>
public class MarketDataUnavailableException : Exception
{
    public const string DefaultMessage = "Сервіс ринкових даних тимчасово недоступний. Спробуйте пізніше.";

    public MarketDataUnavailableException() : base(DefaultMessage) { }

    public MarketDataUnavailableException(string message) : base(message) { }

    public MarketDataUnavailableException(string message, Exception inner) : base(message, inner) { }
}
