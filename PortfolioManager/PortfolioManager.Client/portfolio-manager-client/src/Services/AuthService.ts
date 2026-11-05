import { http } from "./http";
import { SignUpDto } from "../Components/SignUp/SignUpDto";
import SignInDto from "../Components/SignIn/SignInDto";
import AuthResultDto from "../DTO/AuthResultDto";
import UserDto from "../DTO/UserDto";

// withCredentials lets the browser store the API's HttpOnly refresh-token cookie where it allows it.

export const SignUpAsync = async (signUpDto: SignUpDto): Promise<AuthResultDto> => {
    const response = await http.post<AuthResultDto>('auth/signup', signUpDto, { withCredentials: true });
    return response.data;
};

export const SignInAsync = async (signInDto: SignInDto): Promise<AuthResultDto> => {
    const response = await http.post<AuthResultDto>('auth/signin', signInDto, { withCredentials: true });
    return response.data;
};

/**
 * Revokes the refresh token on the server. The access token is passed explicitly because the
 * session has already been cleared locally by the time this runs.
 */
export const SignOutAsync = async (refreshToken: string | null, accessToken: string | null): Promise<void> => {
    await http.post('auth/sign-out', { refreshToken }, {
        withCredentials: true,
        headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined
    });
};

export const GetCurrentUserAsync = async (): Promise<UserDto> => {
    const response = await http.get<UserDto>('user/me');
    return response.data;
};
