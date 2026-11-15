import RiskForecastDto, { RiskFiguresDto } from '../DTO/RiskForecastDto';

/**
 * A risk forecast as the API sends it. The portfolio's and AAA's fractions are the math service's real answer for its
 * test prices (PortfolioManager.MathModule/tests/data/forecast_prices.csv, AAA 60% and BBB 40%); BBB's volatility is
 * made up so the two stocks differ. The money is for a portfolio worth 1000 $.
 */
export const portfolioFigures: RiskFiguresDto = {
    volatility: { value: 0.19072639451457588, low: 0.13741224158896861, high: 0.25199660034255744, previous: 0.171001216127174 },
    valueAtRisk95: { value: 0.020012388234976208, low: 0.012099241818700555, high: 0.025487032830466858, previous: 0.01728680792648556 },
    maxDrawdown: { value: 0.1325217168273489, low: 0.062254510320913205, high: 0.23663287768678837, previous: 0.05902844316068523 },
    outOfDomain: false,
    outOfDomainFeatures: [],
    staleQuotes: false,
};

export const stockFigures: RiskFiguresDto = {
    volatility: { value: 0.2628374499623479, low: 0.18936594101082546, high: 0.347273087197991, previous: 0.23752119495295557 },
    valueAtRisk95: { value: 0.0265174671534612, low: 0.016032131884611004, high: 0.03377165923354852, previous: 0.02068975046045851 },
    maxDrawdown: { value: 0.15501054728138236, low: 0.07281905144763189, high: 0.2767892897341909, previous: 0.11363571123422656 },
    outOfDomain: false,
    outOfDomainFeatures: [],
    staleQuotes: false,
};

export const makeRiskForecast = (overrides: Partial<RiskForecastDto> = {}): RiskForecastDto => ({
    model: { version: 'risk-v1', trainedThrough: '2026-11-13', horizonDays: 63, intervalLevel: 0.8, ageDays: 2, staleAfterDays: 60, stale: false },
    asOf: '2026-11-15',
    tradingDays: 899,
    portfolioValue: 1000,
    portfolio: portfolioFigures,
    losses: {
        valueAtRisk95: { value: 20.0124, low: 12.0992, high: 25.487 },
        maxDrawdown: { value: 132.5217, low: 62.2545, high: 236.6329 },
    },
    assets: [
        { symbol: 'AAA', weight: 0.6, forecast: stockFigures },
        { symbol: 'BBB', weight: 0.4, forecast: { ...stockFigures, volatility: { value: 0.31, low: 0.22, high: 0.41, previous: 0.29 } } },
    ],
    warnings: [],
    ...overrides,
});
