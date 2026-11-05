namespace PortfolioManager.BLL.Exceptions;

/// <summary>Wrong credentials, or a missing, expired or revoked refresh token. Returned as 401.</summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }
}
