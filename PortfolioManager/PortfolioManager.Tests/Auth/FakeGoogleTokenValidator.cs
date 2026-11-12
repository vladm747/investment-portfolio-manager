using System.Collections.Concurrent;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Services.Auth;

namespace PortfolioManager.Tests.Auth;

/// <summary>
/// Stands in for Google: it accepts only the credentials it handed out, each one standing for an ID token Google
/// signed for a chosen person, and refuses everything else as the real validator refuses a forged token.
/// No network, no Google account.
/// </summary>
public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    private readonly ConcurrentDictionary<string, GoogleIdentity> _tokens = new();

    /// <summary>A credential that the fake accepts for this person, as if their browser had just got it from Google.</summary>
    public string Credential(GoogleIdentity identity)
    {
        var credential = "fake-google-id-token-" + Guid.NewGuid().ToString("N");
        _tokens[credential] = identity;
        return credential;
    }

    public Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken = default) =>
        _tokens.TryGetValue(credential, out var identity)
            ? Task.FromResult(identity)
            : Task.FromException<GoogleIdentity>(new AuthenticationFailedException(AuthService.InvalidGoogleTokenMessage));
}
