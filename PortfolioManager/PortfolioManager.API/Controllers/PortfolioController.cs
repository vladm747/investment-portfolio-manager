using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.API.Extensions;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.API.Controllers;

/// <summary>
/// Portfolios: <c>GET/POST api/portfolios</c>, <c>GET/DELETE api/portfolios/{id}</c>. The user is always the one
/// in the access token. A portfolio of another user is answered with 404, exactly like one that does not exist.
/// </summary>
[Authorize]
[ApiController]
[Route("api/portfolios")]
public class PortfolioController : ControllerBase
{
    private const string GetPortfolioRoute = "GetPortfolio";

    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    /// <summary>The signed-in user's portfolios, each with its positions and totals.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var portfolios = await _portfolioService.GetAllAsync(User.GetUserId(), cancellationToken);

        return Ok(portfolios);
    }

    [HttpGet("{id:int}", Name = GetPortfolioRoute)]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        var portfolio = await _portfolioService.GetAsync(User.GetUserId(), id, cancellationToken);

        return Ok(portfolio);
    }

    /// <summary>Creates an empty portfolio for the signed-in user: 201 with the portfolio and its address.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePortfolioDTO createPortfolioDto,
        CancellationToken cancellationToken)
    {
        var created = await _portfolioService.CreatePortfolioAsync(
            User.GetUserId(), createPortfolioDto, cancellationToken);

        return CreatedAtRoute(GetPortfolioRoute, new { id = created.Id }, created);
    }

    /// <summary>Deletes the portfolio and its positions: 204.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _portfolioService.DeleteAsync(User.GetUserId(), id, cancellationToken);

        return NoContent();
    }
}
