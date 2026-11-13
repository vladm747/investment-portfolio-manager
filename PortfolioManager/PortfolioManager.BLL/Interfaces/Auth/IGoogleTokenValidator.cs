namespace PortfolioManager.BLL.Interfaces.Auth;

/// <summary>What a validated Google ID token says about the person. <see cref="Subject"/> is Google's stable id for the account.</summary>
public sealed record GoogleIdentity(string Subject, string? Email, bool EmailVerified, string? Name);

/// <summary>
/// Checks a Google ID token (signature, issuer, audience, expiry). Behind an interface so the tests never reach Google.
/// </summary>
public interface IGoogleTokenValidator
{
    /// <exception cref="PortfolioManager.BLL.Exceptions.AuthenticationFailedException">The token is not a valid, current token for this application.</exception>
    Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken = default);
}
