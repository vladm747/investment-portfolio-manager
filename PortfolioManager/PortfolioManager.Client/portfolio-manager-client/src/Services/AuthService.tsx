import axios from 'axios';

const api = axios.create({
    baseURL: 'https://localhost:44344/api/', // Replace with your backend URL
});

export const SignUpEndpoint = async (endpoint: any) => {
    try {
        const response = await api.get('auth/signup');
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const SignInEndpoint = async (endpoint: any) => {
    try {
        const response = await api.get('auth/signin');
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }

};

export const SignOutEndpoint = async (endpoint: any) => {
    try {
        const response = await api.get('auth/sign-out');
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

