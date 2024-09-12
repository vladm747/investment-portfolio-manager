import axios from 'axios';
import { SignUpDto } from "../Components/SignUp/SignUpDto";
import SignInDto from "../Components/SignIn/SignInDto";
import { useCookies } from "react-cookie";



const api = axios.create({
    baseURL: `${process.env.API_URL}`, // Replace with your backend URL
});

export const SignUpAsync = async (signUpDto: SignUpDto) => {
    try {
        const response = await api.post('auth/signup', signUpDto);
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const SignInAsync = async (signInDto: SignInDto) => {
    try {
        const response = await api.post('auth/signin', signInDto, {
            withCredentials: true
        });
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }

};

export const SignOutAsync = async () => {
    const [cookie, setCookie] = useCookies(['accessToken'])
    axios.interceptors.request.use(
        config => {
            const token = cookie.accessToken; // Retrieve your token dynamically here
            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
            return config;
        },
        error => {
            return Promise.reject(error);
        }
    );
    try {
        const response = await api.get('auth/sign-out');
        return response.data;
    } catch (error) {
        console.error('Error fetching data', error);
        throw error;
    }
};

export const GetCurrentUserAsync = async (token: string) => {
    try {
        const response = await api.get('user/me', {
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        return response.data;
    } catch (error) {
        console.error('Error fetching current user', error);
        throw error;
    }
};


