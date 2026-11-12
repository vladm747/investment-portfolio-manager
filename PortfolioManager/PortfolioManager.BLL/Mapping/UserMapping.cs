using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Mapping;

public static class UserMapping
{
    /// <summary>
    /// What the client may know about a user: never the password hash, the tokens or the lockout state.
    /// Roles are not part of the user row, so the caller passes them when it has them.
    /// </summary>
    public static UserDTO ToDto(this User user, IEnumerable<string>? roles = null) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        Roles = roles?.ToList() ?? new List<string>()
    };
}
