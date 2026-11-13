using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO.Admin;
using PortfolioManager.DAL.Context;

namespace PortfolioManager.API.Controllers;

/// <summary>
/// For administrators only: a signed-in investor gets 403. The role is read from the access token's role claim,
/// and a user gets it when they sign in with an e-mail listed in <c>Auth:AdminEmails</c>.
/// </summary>
[Route("api/admin")]
[Authorize(Roles = RoleSeed.AdminRoleName)]
[ApiController]
public class AdminController(IAdminService service) : ControllerBase
{
    /// <summary>The totals and every user's profile and counts. Read-only; never anybody's portfolio contents.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<AdminUsersDTO>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return Ok(await service.GetUsersAsync(cancellationToken));
    }
}
