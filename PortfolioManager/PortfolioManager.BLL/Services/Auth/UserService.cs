using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Mapping;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class UserService: IUserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> manager)
    {
        _userManager = manager;
    }

    public async Task<UserDTO> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);

        if (user == null)
            throw new UserNotFoundException("User Not Found");

        return user.ToDto(await _userManager.GetRolesAsync(user));
    }

    public async Task<IEnumerable<UserDTO>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        return users.Select(user => user.ToDto()).ToList();
    }

    public async Task<IdentityResult> DeleteAsync(ClaimsPrincipal userPrincipal, string email)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);

        if (user == null)
            throw new UserNotFoundException("User Not Found");

        if (user.Email != email)
            throw new NotAllowedException("You have no permission to delete this user");

        var result = await _userManager.DeleteAsync(user);
        return result;
    }
}
