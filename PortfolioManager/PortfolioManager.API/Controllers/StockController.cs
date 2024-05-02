using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Interfaces;

namespace PortfolioManager.API.Controllers;

[Route("api/stock")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }
    
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok();
    }
}