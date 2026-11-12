using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.API.Extensions;
using PortfolioManager.API.Filters;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.API.Controllers;

/// <summary>
/// Positions, in the portfolio they belong to or by their own id:
/// <c>GET/POST api/portfolios/{portfolioId}/positions</c>, <c>PUT/DELETE api/positions/{id}</c>, and
/// <c>POST api/portfolios/{portfolioId}/prices/refresh</c>. The user is the one in the access token; a position or
/// portfolio of another user is answered with 404, exactly like one that does not exist.
/// </summary>
[Authorize]
[ApiController]
[Route("api")]
public class PositionController : ControllerBase
{
    private readonly IStockService _stockService;
    public PositionController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet("portfolios/{portfolioId:int}/positions")]
    public async Task<IActionResult> GetAllAsync(int portfolioId, CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetAllAsync(User.GetUserId(), portfolioId, cancellationToken);

        return Ok(stocks);
    }

    /// <summary>
    /// Adds a position to the portfolio: 201 with the position. The market data service confirms the ticker and
    /// supplies its name, sector and current price.
    /// </summary>
    [HttpPost("portfolios/{portfolioId:int}/positions")]
    public async Task<IActionResult> CreateAsync(int portfolioId, [FromBody] CreateStockDTO stock,
        CancellationToken cancellationToken)
    {
        var created = await _stockService.CreateAsync(User.GetUserId(), portfolioId, stock, cancellationToken);

        // An absolute path: a relative one would be resolved against .../positions and point somewhere else.
        return Created(Url.Content($"~/api/positions/{created.Id}"), created);
    }

    /// <summary>
    /// Refreshes the current price of every position of the portfolio from the market data service (slow, and it
    /// changes data, so it is a POST): 204 when every price was saved, nothing is saved otherwise.
    /// </summary>
    [HttpPost("portfolios/{portfolioId:int}/prices/refresh")]
    public async Task<IActionResult> RefreshPricesAsync(int portfolioId, CancellationToken cancellationToken)
    {
        await _stockService.UpdatePricesAsync(User.GetUserId(), portfolioId, cancellationToken);

        return NoContent();
    }

    /// <summary>Sets the number of shares of a position. The value and gain follow from it.</summary>
    [HttpPut("positions/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateStockDTO update,
        CancellationToken cancellationToken)
    {
        var stock = await _stockService.UpdateAsync(User.GetUserId(), id, update.Quantity, cancellationToken);

        return Ok(stock);
    }

    [HttpDelete("positions/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _stockService.DeleteAsync(User.GetUserId(), id, cancellationToken);

        return deleted
            ? NoContent()
            : BadRequest(new ErrorDTO { Message = "Позицію не вдалося видалити." });
    }
}
