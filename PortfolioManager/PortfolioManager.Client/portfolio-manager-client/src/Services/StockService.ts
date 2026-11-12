import { http } from "./http";
import StockDto from "../DTO/StockDto";
import CreateStockDto from "../DTO/CreateStockDto";

export const GetAllStocksAsync = async (portfolioId: number): Promise<StockDto[]> => {
    const response = await http.get<StockDto[]>(`portfolios/${portfolioId}/positions`);
    return response.data;
};

/**
 * Refreshes the current price of every position in the portfolio (calls Yahoo Finance, can be slow). It changes
 * data, so it is a POST; answered with 204 once every price is saved, and nothing is saved if one is missing.
 */
export const RefreshPrices = async (portfolioId: number): Promise<void> => {
    await http.post(`portfolios/${portfolioId}/prices/refresh`);
};

/** Adds a position to the portfolio; the portfolio is in the address, not in the body. Answered with 201. */
export const CreateStock = async (portfolioId: number, stockDto: CreateStockDto): Promise<StockDto> => {
    const response = await http.post<StockDto>(`portfolios/${portfolioId}/positions`, stockDto);
    return response.data;
};

/** Sets the number of shares of a position; the server recalculates its value and gain. */
export const UpdateStockQuantity = async (stockId: number, quantity: number): Promise<StockDto> => {
    const response = await http.put<StockDto>(`positions/${stockId}`, { quantity });
    return response.data;
};

/** Answered with 204. */
export const DeleteStock = async (stockId: number): Promise<void> => {
    await http.delete(`positions/${stockId}`);
};
