namespace PortfolioManager.MathModule.Client;

/// <summary>
/// The math service understood the request and refused it with 400 or 422, for example
/// <c>not_enough_history</c> (a recently listed stock) or <c>optimisation_failed</c>. Returned as 400.
/// The message is the math service's own text (English).
/// </summary>
public class MathModuleRejectedException : Exception
{
    /// <summary>The service's error code, such as <c>not_enough_history</c>.</summary>
    public string Code { get; }

    public MathModuleRejectedException(string code, string message) : base(message)
    {
        Code = code;
    }
}
