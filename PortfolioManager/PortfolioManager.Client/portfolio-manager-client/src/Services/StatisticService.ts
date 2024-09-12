import axios from 'axios';

const api = axios.create({
    baseURL: `${process.env.API_URL}`, // Replace with your backend URL
    timeout: 100000
});

export const GetOptimisationData = async (token: string, portfolioId: number) => {
    try {
        const response = await api.get('opt/' + portfolioId, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        }).then((response) => {
            return response;

        }).catch((error) => {
            throw error;
        });

        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
}

export const GetGrowthStatsData = async (token: string, portfolioId: number) => {
    try {
        const response = await api.get('growth/' + portfolioId, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        }).then((response) => {
            return response;

        }).catch((error) => {
            throw error;
        });

        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
}
