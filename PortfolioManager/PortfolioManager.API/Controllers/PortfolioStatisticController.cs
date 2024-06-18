using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Interfaces;

namespace PortfolioManager.API.Controllers;
[Authorize]
[Route("api/statistics")]
public class PortfolioStatisticController: ControllerBase
{
    private readonly IPortfolioStatisticService _portfolioStatisticService;
    public PortfolioStatisticController(IPortfolioStatisticService portfolioStatisticService)
    {
        _portfolioStatisticService = portfolioStatisticService;
    }
    
    [HttpGet("opt/{portfolioId}")]
    public async Task<IActionResult> GetMetrics(int portfolioId)
    {
        var metrics = _portfolioStatisticService.GetMetrics(portfolioId);
        
        return Ok(metrics);
    }
    
    [HttpGet("growth/{portfolioId}")]
    public async Task<IActionResult> GetGrowthStatistic(int portfolioId)
    {
        var growthStats  = await _portfolioStatisticService.GetPortfolioGrowsStats(portfolioId);
        
        return Ok(growthStats);
    }
}