using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;

    public RoleService(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }
    public async Task<IEnumerable<IdentityRole>> GetRoles()
    {
        return await _roleManager.Roles.ToListAsync();
    }

    public async Task<IEnumerable<string>> GetRoles(User user)
    {
        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task CreateRole(string roleName)
    {
        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (!result.Succeeded)
        {
            throw new Exception($"Role cant be created: {roleName}");
        }
    }
}
