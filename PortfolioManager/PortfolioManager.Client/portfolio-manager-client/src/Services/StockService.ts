import { http } from "./http";
import StockDto from "../DTO/StockDto";
import CreateStockDto from "../DTO/CreateStockDto";

export const GetAllStocksAsync = async (portfolioId: number): Promise<StockDto[]> => {
    const response = await http.get<StockDto[]>('stock/' + portfolioId);
    return response.data;
};

/** Refreshes current prices of every position in the portfolio (calls Yahoo Finance, can be slow). */
export const UpdateStockInfo = async (portfolioId: number) => {
    const response = await http.get('stock/info/' + portfolioId);
    return response.data;
};

export const CreateStock = async (stockDto: CreateStockDto) => {
    const response = await http.post('stock', stockDto);
    return response.data;
};

export const DeleteStock = async (stockId: number) => {
    const response = await http.delete('stock/' + stockId);
    return response.data;
};
