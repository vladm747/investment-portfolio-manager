using System.Globalization;
using System.Text.RegularExpressions;
using PortfolioManager.BLL.Models;
using PortfolioManager.BLL.Valuation;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.BLL.Mapping;

/// <summary>From the math service's forecast (<see cref="ForecastResponse"/>) to what the API returns.</summary>
public static partial class RiskForecastMapping
{
    /// <summary>The forecast of a portfolio worth <paramref name="portfolioValue"/> now, with the losses in money.</summary>
    public static PortfolioRiskForecast ToModel(this ForecastResponse forecast, decimal portfolioValue)
    {
        var portfolio = forecast.Portfolio.ToFigures();

        return new PortfolioRiskForecast(
            forecast.Model.ToModel(),
            forecast.AsOf,
            forecast.TradingDays,
            portfolioValue,
            portfolio,
            new PotentialLosses(
                Losses(portfolioValue, portfolio.ValueAtRisk95),
                Losses(portfolioValue, portfolio.MaxDrawdown)),
            forecast.Assets.Select(asset => new AssetRisk(asset.Symbol, asset.Weight, asset.Forecast?.ToFigures())).ToList(),
            forecast.Warnings.ToList());
    }

    public static RiskModelInfo ToModel(this ForecastModelInfo model) =>
        new(model.Version, model.TrainedThrough, model.HorizonDays, model.IntervalLevel, model.AgeDays,
            model.StaleAfterDays, model.Stale);

    public static RiskFigures ToFigures(this RiskForecast forecast) =>
        new(forecast.Volatility.ToModel(), forecast.ValueAtRisk95.ToModel(), forecast.MaxDrawdown.ToModel(),
            forecast.OutOfDomain, forecast.OutOfDomainFeatures.ToList(), forecast.StaleQuotes);

    private static RiskMetric ToModel(this MetricForecast metric) =>
        new(metric.Value, metric.Low, metric.High, metric.Previous);

    private static MoneyRange Losses(decimal portfolioValue, RiskMetric metric) =>
        new(Amount(portfolioValue, metric.Value), Amount(portfolioValue, metric.Low), Amount(portfolioValue, metric.High));

    private static decimal? Amount(decimal portfolioValue, double? fraction) =>
        fraction is { } value && double.IsFinite(value) ? Money.Round(portfolioValue * (decimal)value) : null;

    /// <summary>
    /// The Ukrainian text of a refusal the user can act on: a stock listed too recently (<c>not_enough_history</c>) or
    /// prices with gaps (<c>forecast_failed</c>). Other refusals mean a bug in this API, so they keep the service's
    /// own text.
    /// </summary>
    public static bool TryExplain(MathModuleRejectedException rejection, out string message)
    {
        message = rejection.Code switch
        {
            "not_enough_history" => NotEnoughHistory(rejection.Message),
            "forecast_failed" =>
                "Не вдалося обчислити прогноз ризику: в історії цін акцій портфеля є пропуски. Спробуйте пізніше.",
            _ => string.Empty
        };
        return message.Length > 0;
    }

    private static string NotEnoughHistory(string serviceMessage)
    {
        const string rule = "Для прогнозу ризику потрібна щонайменше річна історія цін кожної акції портфеля.";

        // The math service names the youngest stock (app/forecast/portfolio.py, NotEnoughHistory). If that text ever
        // changes, the rule alone is still right.
        var match = NotEnoughHistoryText().Match(serviceMessage);
        if (!match.Success
            || !DateOnly.TryParseExact(match.Groups["since"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var since))
            return rule;

        var sinceText = since.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return $"{rule} {match.Groups["symbol"].Value} має ціни лише з {sinceText}: " +
               $"{match.Groups["days"].Value} із {match.Groups["needed"].Value} потрібних торгових днів.";
    }

    [GeneratedRegex(
        @"^Not enough price history: (?<symbol>\S+) has prices only from (?<since>\d{4}-\d{2}-\d{2}), which gives (?<days>\d+) trading days; .* at least (?<needed>\d+)\.$")]
    private static partial Regex NotEnoughHistoryText();
}
