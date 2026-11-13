using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO.Auth;

/// <summary>
/// What the browser got from the "Sign in with Google" button: Google's ID token, a signed JWT.
/// There is deliberately no role, name or email here: the API takes them from the validated token, so nobody
/// can sign in as someone else or register themselves as an admin.
/// </summary>
public class GoogleSignInDTO
{
    /// <summary>A real ID token is about 1 to 2 KB; the cap only stops an absurd request from reaching the validator.</summary>
    public const int MaxCredentialLength = 8192;

    [Required(ErrorMessage = "Не вдалося отримати дані від Google. Спробуйте ще раз.")]
    [MaxLength(MaxCredentialLength, ErrorMessage = "Не вдалося отримати дані від Google. Спробуйте ще раз.")]
    public string Credential { get; set; } = string.Empty;
}
