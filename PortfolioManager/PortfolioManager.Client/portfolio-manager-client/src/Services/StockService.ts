import axios from 'axios';
import StockDto from "../DTO/StockDto";
import { useCookies } from "react-cookie";
import CreateStockDto from "../DTO/CreateStockDto";

const api = axios.create({
    baseURL: `${process.env.API_URL}`, // Replace with your backend URL
    timeout: 10000000
});

export const GetAllStocksAsync = async (token: string, portfolioId: number) => {
    try {
        const response = await api.get('stock/' + portfolioId, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const GetStockAsync = async (token: string, id: number) => {
    try {
        const response = await api.get('stock/' + id, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const UpdateStockInfo = async (token: string, portfolioId: number) => {
    try {
        const response = await api.get('stock/info/' + portfolioId, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const CreateStock = async (token: string, stockDto: CreateStockDto) => {
    try {
        const response = await api.post('stock', stockDto, {
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};


export const DeleteStock = async (token: string, portfolioId: number) => {
    try {
        const response = await api.delete('stock/' + portfolioId, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};
