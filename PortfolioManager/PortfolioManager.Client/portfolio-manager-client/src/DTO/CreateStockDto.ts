export default interface CreateStockDto {
    portfolioId: number;
    symbol: string;
    quantity: number;
    entryPrice: number;
    entryDate: Date;
}
