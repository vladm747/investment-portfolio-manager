using Microsoft.AspNetCore.Identity;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface IRoleService
{
    Task<IEnumerable<IdentityRole>> GetRoles();
    Task<IEnumerable<string>> GetRoles(User user);
    Task CreateRole(string roleName);
}