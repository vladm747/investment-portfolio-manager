﻿using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetAllAsync(int portfolioId)
    {
        var stocks = await _stockService.GetAllAsync(portfolioId);
        
        return Ok(stocks);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateStock(int stockId, int quantity)
    {
        var stock = await _stockService.UpdateAsync(stockId, quantity);
        
        if(stock.Quantity != quantity)
            return BadRequest("Stock not updated");
        
        return Ok(stock);
    }
    
    public async Task<IActionResult> UpdateInfo(int portfolioId)
    {
        await _stockService.UpdateInfo(portfolioId);

        return Ok();
    }
    [HttpPost]
    public async Task<IActionResult> CreateStock(int portfolioId, int quantity, string symbol, decimal entryPrice)
    {
        var result = await _stockService.CreateAsync(portfolioId, quantity, symbol, entryPrice);
        
        return Ok(result);
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteStock(int stockId)
    {
        var result = await _stockService.DeleteAsync(stockId);
        
        if(!result)
            return BadRequest("Stock not deleted");
        
        return Ok();
    }
}