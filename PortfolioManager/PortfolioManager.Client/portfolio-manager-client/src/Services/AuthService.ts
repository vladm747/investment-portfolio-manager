import { http } from "./http";
import AuthResultDto from "../DTO/AuthResultDto";
import UserDto from "../DTO/UserDto";

// withCredentials lets the browser store the API's HttpOnly refresh-token cookie where it allows it.

/**
 * The only way to sign in. `credential` is the ID token the "Sign in with Google" button gives the page; the API
 * checks it with Google and answers with its own tokens. The first sign-in creates the account.
 */
export const SignInWithGoogleAsync = async (credential: string): Promise<AuthResultDto> => {
    const response = await http.post<AuthResultDto>('auth/google', { credential }, { withCredentials: true });
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
