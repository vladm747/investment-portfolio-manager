using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.API.Extensions;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.API.Controllers;
[Authorize]
[Route("api")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    /// <summary>
    /// Portfolios of the signed-in user. POST is kept only so the client that is deployed now keeps
    /// working until it is redeployed; the user id it sends in the form is ignored.
    /// </summary>
    [HttpGet("portfolios")]
    [HttpPost("portfolios")]
    public async Task<IActionResult> GetAllAsync()
    {
        var portfolios = await _portfolioService.GetAllAsync(User.GetUserId());

        return Ok(portfolios);
    }
    [HttpGet("portfolio/{id}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        var portfolio = await _portfolioService.GetAsync(id);

        if(portfolio == null)
            return NotFound();

        return Ok(portfolio);
    }

    [HttpPost("portfolio")]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePortfolioDTO createPortfolioDto)
    {
        // The owner always comes from the access token, never from the request body.
        createPortfolioDto.UserId = User.GetUserId();
        var createdPortfolio = await _portfolioService.CreatePortfolioAsync(createPortfolioDto);

        return Ok(createdPortfolio);
    }

    [HttpDelete("portfolio/{portfolioId}")]
    public async Task<IActionResult> CreateAsync(int portfolioId)
    {
        var createdPortfolio = await _portfolioService.DeleteAsync(portfolioId);

        return Ok(createdPortfolio);
    }
}
