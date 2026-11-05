import { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { http } from '../Services/http';
import AuthResultDto from '../DTO/AuthResultDto';

export type FakeReply = { status: number; data?: unknown } | 'network-error';

/** Replaces the network for the shared axios instance and records every request it receives. */
export function fakeApi(reply: (request: InternalAxiosRequestConfig) => FakeReply) {
    const requests: InternalAxiosRequestConfig[] = [];

    http.defaults.adapter = async (config) => {
        requests.push(config);
        const result = reply(config);

        if (result === 'network-error') {
            throw new AxiosError('Network Error', AxiosError.ERR_NETWORK, config);
        }

        const response: AxiosResponse = {
            data: result.data,
            status: result.status,
            statusText: '',
            headers: {},
            config,
        };

        if (result.status >= 400) {
            throw new AxiosError('Request failed', AxiosError.ERR_BAD_REQUEST, config, undefined, response);
        }

        return response;
    };

    return requests;
}

export const authHeader = (request: InternalAxiosRequestConfig) => request.headers.get('Authorization');

export const bodyOf = (request: InternalAxiosRequestConfig) => JSON.parse(request.data);

export const testUser = { id: 'user-1', fullName: 'Олена Коваль', email: 'olena@example.com', roles: ['investor'] };

/** Tokens as the API would return them; n tells apart successive sign-ins and refreshes. */
export const authResult = (n: number): AuthResultDto => ({
    accessToken: `access-${n}`,
    accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    refreshToken: `refresh-${n}`,
    refreshTokenExpiresAt: '2030-01-07T00:00:00Z',
    user: testUser,
});
