using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface IUserService
{
    Task<UserDTO> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);
    Task<IEnumerable<UserDTO>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IdentityResult> DeleteAsync(ClaimsPrincipal user, string email);
}
