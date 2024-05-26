using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Interfaces;

namespace PortfolioManager.API.Controllers;

[Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    
    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var portfolios = await _portfolioService.GetAllAsync();
        
        return Ok(portfolios);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        var portfolio = await _portfolioService.GetAsync(id);
        
        if(portfolio == null)
            return NotFound();
        
        return Ok(portfolio);
    }
}