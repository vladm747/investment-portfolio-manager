/**
 * The risk forecast of the trained model: GET api/portfolios/{id}/forecast (PortfolioManager.BLL/Models/PortfolioRiskForecast.cs).
 * Risk figures are fractions (0.19 is 19%); money is in the portfolio's currency, as the totals are.
 */

/** A forecast with its 80% interval, and the same metric over the last three months. null where undefined. */
export interface RiskMetricDto {
    value: number | null;
    low: number | null;
    high: number | null;
    previous: number | null;
}

export interface RiskFiguresDto {
    /** Annualised. */
    volatility: RiskMetricDto;
    /** One trading day, 95% confidence. */
    valueAtRisk95: RiskMetricDto;
    /** Over the next three months. */
    maxDrawdown: RiskMetricDto;
    /** Unlike anything the model was trained on: the forecast is less reliable. */
    outOfDomain: boolean;
    /** The model's names of the features outside the training range, such as "vol_252". */
    outOfDomainFeatures: string[];
    /** Many days without a price change. */
    staleQuotes: boolean;
}

export interface RiskModelDto {
    version: string;
    /** yyyy-MM-dd: the last price the model was trained on. */
    trainedThrough: string;
    horizonDays: number;
    /** 0.8 */
    intervalLevel: number;
    ageDays: number;
    staleAfterDays: number;
    stale: boolean;
}

export interface MoneyRangeDto {
    value: number | null;
    low: number | null;
    high: number | null;
}

export interface AssetRiskDto {
    symbol: string;
    /** Share of the portfolio's value, 0 to 1. */
    weight: number;
    /** null when the stock's own recent prices have gaps; the portfolio is still forecast. */
    forecast: RiskFiguresDto | null;
}

export default interface RiskForecastDto {
    model: RiskModelDto;
    /** yyyy-MM-dd: the last trading day of the prices the forecast starts from. */
    asOf: string;
    tradingDays: number;
    portfolioValue: number;
    portfolio: RiskFiguresDto;
    losses: { valueAtRisk95: MoneyRangeDto; maxDrawdown: MoneyRangeDto };
    assets: AssetRiskDto[];
    /** model_stale, portfolio_out_of_domain, assets_out_of_domain, assets_without_forecast */
    warnings: string[];
}

/** The riskForecast of GET api/portfolios/{id}/optimisation: null when the model could not make it. */
export interface OptimisationRiskForecastDto {
    model: RiskModelDto;
    asOf: string;
    base: RiskFiguresDto;
    optimised: RiskFiguresDto;
}
