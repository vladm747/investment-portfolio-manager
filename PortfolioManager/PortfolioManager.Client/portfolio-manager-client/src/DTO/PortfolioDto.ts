import StockDto from './StockDto';

export default interface PortfolioDto {
    id: number;
    name: string;
    totalEntryPrice: number;
    totalCurrentPrice: number;
    totalGain: number;
    totalGainPercentage: number;
    userId: string;
    stocks: StockDto[];
}
