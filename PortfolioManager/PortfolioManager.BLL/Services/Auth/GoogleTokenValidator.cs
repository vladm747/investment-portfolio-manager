using Google.Apis.Auth;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;

namespace PortfolioManager.BLL.Services.Auth;

/// <summary>
/// Google's own library does the work: it checks the signature against Google's published keys (cached), the issuer,
/// the expiry, and that the token was issued for this application's client ID (the audience).
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleJsonWebSignature.ValidationSettings _validation;

    public GoogleTokenValidator(GoogleAuthSettings settings)
    {
        _validation = new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { settings.ClientId } };
    }

    public async Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(credential, _validation).WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidJwtException or FormatException or ArgumentException or Newtonsoft.Json.JsonException)
        {
            // Forged, expired or issued for another application: InvalidJwtException. Not a JWT at all: the library
            // lets the parser's own exceptions through (empty text, bad base64, bad JSON). All the same to the caller,
            // who must get a 401 and not a 500. A failure to reach Google's keys is none of these and stays an error.
            throw new AuthenticationFailedException(AuthService.InvalidGoogleTokenMessage);
        }

        return new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.Name);
    }
}
