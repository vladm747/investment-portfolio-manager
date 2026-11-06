export default interface CreateStockDto {
    portfolioId: number;
    symbol: string;
    quantity: number;
    entryPrice: number;
    /** «2024-03-12T00:00:00»: a calendar day without a time zone, so it is the same day on every machine. */
    entryDate: Date | string;
}
