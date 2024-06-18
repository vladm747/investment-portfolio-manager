using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.API.Controllers;

[Authorize]
[Route("api/stock")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;
    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }
    
    [HttpGet("{portfolioId}")]
    public async Task<IActionResult> GetAllAsync(int portfolioId)
    {
        var stocks = await _stockService.GetAllAsync(portfolioId);
        
        return Ok(stocks);
    }
    
    [HttpPut("{stockId}/{quantity}")]
    public async Task<IActionResult> UpdateStock(int stockId, int quantity)
    {
        var stock = await _stockService.UpdateAsync(stockId, quantity);
        
        if(stock.Quantity != quantity)
            return BadRequest("Stock not updated");
        
        return Ok(stock);
    }
    [HttpGet("info/{portfolioId}")]
    public async Task<IActionResult> UpdateInfo(int portfolioId)
    {
        await _stockService.UpdateInfo(portfolioId);

        return Ok();
    }
    [HttpPost]
    public async Task<IActionResult> CreateStock([FromBody] CreateStockDTO stock)
    {
        var result = await _stockService.CreateAsync(stock.PortfolioId, stock.Quantity, stock.Symbol, stock.EntryPrice, stock.EntryDate);
        
        return Ok(result);
    }
    
    [HttpDelete("{stockId}")]
    public async Task<IActionResult> DeleteStock(int stockId)
    {
        var result = await _stockService.DeleteAsync(stockId);
        
        if(!result)
            return BadRequest("Stock not deleted");
        
        return Ok();
    }
}