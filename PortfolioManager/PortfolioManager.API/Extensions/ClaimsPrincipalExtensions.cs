using System.Security.Claims;
using PortfolioManager.BLL.Exceptions;

namespace PortfolioManager.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id, taken from the access token rather than from anything the client sends.</summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthenticationFailedException("Не вдалося визначити користувача. Увійдіть знову.");
}
