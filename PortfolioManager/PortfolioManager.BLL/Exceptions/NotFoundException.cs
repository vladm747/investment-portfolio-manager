namespace PortfolioManager.BLL.Exceptions;

/// <summary>
/// The record does not exist, or it belongs to another user. The two cases look the same on purpose:
/// telling them apart would let anyone find out which ids exist. Returned as 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException Portfolio() => new("Портфель не знайдено.");

    public static NotFoundException Position() => new("Позицію не знайдено.");
}
