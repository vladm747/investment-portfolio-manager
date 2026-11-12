/** The body of a new position. The portfolio is in the address (`portfolios/{id}/positions`), so it is not here. */
export default interface CreateStockDto {
    symbol: string;
    quantity: number;
    entryPrice: number;
    /** «2024-03-12T00:00:00»: a calendar day without a time zone, so it is the same day on every machine. */
    entryDate: Date | string;
}
