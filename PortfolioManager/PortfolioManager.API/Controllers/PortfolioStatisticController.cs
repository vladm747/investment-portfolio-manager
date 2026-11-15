using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioManager.API.Extensions;
using PortfolioManager.BLL.Interfaces;

namespace PortfolioManager.API.Controllers;

/// <summary>
/// Statistics of a portfolio, as sub-resources of it: <c>GET api/portfolios/{portfolioId}/growth</c>,
/// <c>GET api/portfolios/{portfolioId}/optimisation</c> and <c>GET api/portfolios/{portfolioId}/forecast</c>. The user is
/// the one in the access token; a portfolio of another user is answered with 404, exactly like one that does not exist.
/// </summary>
[Authorize]
[ApiController]
[Route("api/portfolios/{portfolioId:int}")]
public class PortfolioStatisticController: ControllerBase
{
    private readonly IPortfolioStatisticService _portfolioStatisticService;
    public PortfolioStatisticController(IPortfolioStatisticService portfolioStatisticService)
    {
        _portfolioStatisticService = portfolioStatisticService;
    }

    /// <summary>
    /// The current portfolio against the one with the best Sharpe ratio: nine comparison metrics (each with a
    /// stable <c>metricKey</c>) and the two sets of weights.
    /// </summary>
    [HttpGet("optimisation")]
    public async Task<IActionResult> GetOptimisation(int portfolioId, CancellationToken cancellationToken)
    {
        var comparison = await _portfolioStatisticService.GetMetricsAsync(User.GetUserId(), portfolioId, cancellationToken);

        return Ok(comparison);
    }

    /// <summary>
    /// The risk the trained model expects over the next three months (63 trading days): annualised volatility,
    /// one-day VaR 95 % and maximum drawdown, each with its 80 % interval and its value over the last three months,
    /// for the portfolio and each stock, plus the losses in money and warnings (an old model, an unusual stock).
    /// </summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(int portfolioId, CancellationToken cancellationToken)
    {
        var forecast = await _portfolioStatisticService.GetForecastAsync(User.GetUserId(), portfolioId, cancellationToken);

        return Ok(forecast);
    }

    /// <summary>What the portfolio was worth on each day since its earliest purchase.</summary>
    [HttpGet("growth")]
    public async Task<IActionResult> GetGrowth(int portfolioId, CancellationToken cancellationToken)
    {
        var growth = await _portfolioStatisticService.GetGrowthAsync(User.GetUserId(), portfolioId, cancellationToken);

        return Ok(growth);
    }
}
