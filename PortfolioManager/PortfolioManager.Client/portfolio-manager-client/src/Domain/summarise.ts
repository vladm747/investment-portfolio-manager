import StockDto from "../DTO/StockDto";

export interface PortfolioSummary {
    /** What was paid for the positions: entry price × quantity. */
    invested: number;
    /** What they are worth now. */
    current: number;
    gain: number;
    /** Gain as a percentage of `invested`; 0 for an empty portfolio. */
    gainPercentage: number;
}

/**
 * Totals of a portfolio, computed from its positions so they are always in step with the
 * positions table (the totals stored on the portfolio are only refreshed by the server).
 */
export function summarise(stocks: StockDto[]): PortfolioSummary {
    const invested = stocks.reduce((sum, stock) => sum + stock.entryPrice * stock.quantity, 0);
    const current = stocks.reduce((sum, stock) => sum + stock.totalCurrentPrice, 0);
    const gain = current - invested;

    return {
        invested,
        current,
        gain,
        gainPercentage: invested > 0 ? (gain / invested) * 100 : 0,
    };
}
