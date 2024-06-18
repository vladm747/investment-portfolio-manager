using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    
    [HttpPost("portfolios")]
    public async Task<IActionResult> GetAllAsync([FromForm] string userId)
    {
        var portfolios = await _portfolioService.GetAllAsync(userId);
        
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