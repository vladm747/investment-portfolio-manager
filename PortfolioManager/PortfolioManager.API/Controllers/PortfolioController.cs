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
    public IActionResult GetAllAsync()
    {
        return Ok("It works!");
    }
}