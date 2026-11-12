using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.DAL.Context;

namespace PortfolioManager.API.Controllers;

[Route("api/user")]
[Authorize]
[ApiController]
public class UserController(IUserService service) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await service.GetCurrentUserAsync(HttpContext.User);

        return Ok(result);
    }

    /// <summary>Everybody's name and email: for administrators only. A signed-in investor gets 403.</summary>
    [HttpGet("users")]
    [Authorize(Roles = RoleSeed.AdminRoleName)]
    public async Task<IActionResult> GetAllUsersAsync(CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpDelete("user")]
    public async Task<IActionResult> DeleteAccount(
        [Required(ErrorMessage = "Вкажіть пошту.")] string email)
    {
        var result = await service.DeleteAsync(HttpContext.User, email);

        if (!result.Succeeded)
            throw new ValidationFailedException(result.Errors.Select(error => error.Description));

        return Ok();
    }
}
