namespace PortfolioManager.BLL.Exceptions;

/// <summary>The request was understood but its data is not acceptable. Returned as 400 with the list of errors.</summary>
public class ValidationFailedException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationFailedException(IEnumerable<string> errors)
        : this(errors.Distinct().ToList()) { }

    private ValidationFailedException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}
