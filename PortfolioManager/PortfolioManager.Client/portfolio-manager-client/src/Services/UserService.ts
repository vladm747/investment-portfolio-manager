import axios from 'axios';
import {UserDto} from "../Components/SignIn/UserDto";
import {useCookies} from "react-cookie";

const [cookie, setCookie] = useCookies(['accessToken'])

const api = axios.create({
    baseURL: 'https://localhost:44344/api/'
});

export const GetCurrentUser = async (token: string) => {
    try {
        const response = await api.get('user/me',{
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
