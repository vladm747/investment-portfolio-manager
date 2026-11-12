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

/** An administrator: the API gives the admin role to e-mails on its Auth:AdminEmails list, next to investor. */
export const adminUser = { id: 'admin-1', fullName: 'Адмін Тестовий', email: 'admin@example.com', roles: ['investor', 'admin'] };

/** Tokens as the API would return them; n tells apart successive sign-ins and refreshes. */
export const authResult = (n: number, user: AuthResultDto['user'] = testUser): AuthResultDto => ({
    accessToken: `access-${n}`,
    accessTokenExpiresAt: '2030-01-01T00:00:00Z',
    refreshToken: `refresh-${n}`,
    refreshTokenExpiresAt: '2030-01-07T00:00:00Z',
    user,
});

/** What the "Sign in with Google" script puts on the page: records the config it was given and the buttons drawn. */
export function installFakeGoogle() {
    const buttons: { parent: HTMLElement; options: Record<string, unknown> }[] = [];
    const initialisations: { client_id: string; callback: (response: { credential?: string }) => void }[] = [];

    const id = {
        initialize: vi.fn((config: (typeof initialisations)[number]) => {
            initialisations.push(config);
        }),
        renderButton: vi.fn((parent: HTMLElement, options: object) => {
            const drawn = options as Record<string, unknown>;
            parent.setAttribute('data-google-theme', String(drawn.theme));
            buttons.push({ parent, options: drawn });
        }),
    };
    window.google = { accounts: { id } };

    return {
        id,
        buttons,
        initialisations,
        /** The user picks an account: Google calls the callback it was initialised with. */
        chooseAccount: (credential?: string) => initialisations[initialisations.length - 1].callback({ credential }),
    };
}

export function removeFakeGoogle() {
    delete window.google;
}
