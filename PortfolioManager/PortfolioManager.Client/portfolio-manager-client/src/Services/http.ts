import axios, { AxiosError } from 'axios';
import AuthResultDto from "../DTO/AuthResultDto";
import { sessionStore } from "../Auth/session";

declare module 'axios' {
    interface AxiosRequestConfig {
        /** Set on a request that is being sent again after a token refresh, so it is never retried twice. */
        retriedAfterRefresh?: boolean;
    }
}

/** The one axios instance every service uses. It adds the access token and renews it when it expires. */
export const http = axios.create({
    baseURL: process.env.REACT_APP_URL,
});

const AUTH_ENDPOINTS = ['auth/signin', 'auth/signup', 'auth/refresh-token', 'auth/sign-out'];

const isAuthEndpoint = (url?: string) =>
    !!url && AUTH_ENDPOINTS.some((endpoint) => url.endsWith(endpoint));

export const isUnauthorized = (error: unknown) =>
    axios.isAxiosError(error) && error.response?.status === 401;

http.interceptors.request.use((config) => {
    const token = sessionStore.getAccessToken();
    if (token && !config.headers.has('Authorization')) {
        config.headers.set('Authorization', `Bearer ${token}`);
    }
    return config;
});

http.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const request = error.config;

        if (!isUnauthorized(error) || !request || request.retriedAfterRefresh || isAuthEndpoint(request.url)) {
            return Promise.reject(error);
        }

        try {
            await refreshSession();
        } catch {
            // The session is over; sessionStore.end() has already sent the user back to the sign-in page.
            return Promise.reject(error);
        }

        request.retriedAfterRefresh = true;
        request.headers.delete('Authorization'); // the request interceptor adds the new token
        return http(request);
    }
);

let refreshInFlight: Promise<AuthResultDto> | null = null;

/**
 * Gets a new access token with the stored refresh token. Requests that fail at the same moment
 * share one call, because each refresh replaces the refresh token and a second call with the
 * old one would be rejected.
 */
export function refreshSession(): Promise<AuthResultDto> {
    if (!refreshInFlight) {
        refreshInFlight = requestNewTokens().finally(() => {
            refreshInFlight = null;
        });
    }
    return refreshInFlight;
}

async function requestNewTokens(): Promise<AuthResultDto> {
    const usedToken = sessionStore.getRefreshToken();
    if (!usedToken) {
        sessionStore.end();
        throw new Error('No refresh token');
    }

    try {
        return await postRefresh(usedToken);
    } catch (error) {
        if (!isUnauthorized(error)) {
            // Offline or a server error: keep the session, the next request will try again.
            throw error;
        }

        // Another tab may have refreshed (and so replaced the token) while this request was on its way.
        const latestToken = sessionStore.getRefreshToken();
        if (latestToken && latestToken !== usedToken) {
            try {
                return await postRefresh(latestToken);
            } catch (retryError) {
                if (!isUnauthorized(retryError)) {
                    throw retryError;
                }
            }
        }

        sessionStore.end();
        throw error;
    }
}

async function postRefresh(refreshToken: string): Promise<AuthResultDto> {
    const response = await http.post<AuthResultDto>('auth/refresh-token', { refreshToken }, {
        withCredentials: true
    });
    sessionStore.start(response.data);
    return response.data;
}
