namespace PortfolioManager.BLL.Exceptions;

public class UserTokenUpdateException: Exception
{
    public UserTokenUpdateException() { }

    public UserTokenUpdateException(string message) : base(message) { }

    public UserTokenUpdateException(string message, Exception inner) : base(message, inner) { }
}