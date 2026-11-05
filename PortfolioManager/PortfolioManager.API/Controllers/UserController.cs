using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsersAsync()
    {
        var result = await service.GetAllAsync();
        return Ok(result);
    }
        
    [HttpDelete("user")]
    public async Task<IActionResult> DeleteAccount(string email)
    {
        var result = await service.DeleteAsync(HttpContext.User, email);
        
        switch (result.Succeeded)
        {
            case true:
                return Ok();
            case false:
                return BadRequest(result.Errors.ToString());
        }
    } 
}