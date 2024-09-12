import axios from 'axios';
import PortfolioDto from "../DTO/PortfolioDto";
import { CreatePortfolioDto } from "../DTO/CreatePortfolioDto";

const api = axios.create({
    baseURL: `${process.env.REACT_APP_URL}`, // Replace with your backend URL
});

export const GetAllPortfoliosAsync = async (token: string, userId: string) => {
    try {
        const formData = new URLSearchParams();
        formData.append('userId', userId);
        const response = await api.post('portfolios/', formData, {
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/x-www-form-urlencoded'
            }
        });

        return await response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const GetPortfolioAsync = async (token: string, id: number) => {
    try {
        const response = await api.get('portfolio/' + id, {
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

export const CreatePortfolio = async (token: string, portfolioDto: CreatePortfolioDto) => {
    try {
        const response = await api.post('portfolio', portfolioDto, {
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

export const DeletePortfolio = async (token: string, portfolioId: number) => {
    try {
        const response = await api.delete(`portfolio/${portfolioId}`, {
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
