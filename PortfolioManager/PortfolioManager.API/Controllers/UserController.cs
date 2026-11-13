using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;

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
