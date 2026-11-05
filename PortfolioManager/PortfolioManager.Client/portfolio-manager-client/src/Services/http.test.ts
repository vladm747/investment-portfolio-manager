import { http, refreshSession } from './http';
import { REFRESH_TOKEN_KEY, sessionStore } from '../Auth/session';
import { authHeader, authResult, bodyOf, fakeApi } from '../testing/fakeApi';

beforeEach(() => {
    sessionStore.end();
    window.localStorage.clear();
});

test('adds the access token to every request', async () => {
    sessionStore.start(authResult(1));
    const requests = fakeApi(() => ({ status: 200, data: [] }));

    await http.get('portfolios');

    expect(authHeader(requests[0])).toBe('Bearer access-1');
});

test('an expired access token is refreshed once and the failed requests are sent again', async () => {
    sessionStore.start(authResult(1));
    const requests = fakeApi((request) => {
        if (request.url === 'auth/refresh-token') {
            return { status: 200, data: authResult(2) };
        }
        return authHeader(request) === 'Bearer access-2' ? { status: 200, data: 'ok' } : { status: 401 };
    });

    const responses = await Promise.all([http.get('portfolios'), http.get('stock/1')]);

    expect(responses.map((response) => response.data)).toEqual(['ok', 'ok']);
    const refreshes = requests.filter((request) => request.url === 'auth/refresh-token');
    expect(refreshes).toHaveLength(1);
    expect(bodyOf(refreshes[0])).toEqual({ refreshToken: 'refresh-1' });
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('refresh-2');
});

test('when the refresh token is rejected the session ends', async () => {
    sessionStore.start(authResult(1));
    fakeApi(() => ({ status: 401 }));

    await expect(http.get('portfolios')).rejects.toMatchObject({ response: { status: 401 } });

    expect(sessionStore.get()).toBeNull();
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
});

test('when the server cannot be reached during a refresh the session is kept', async () => {
    sessionStore.start(authResult(1));
    fakeApi((request) => (request.url === 'auth/refresh-token' ? 'network-error' : { status: 401 }));

    await expect(http.get('portfolios')).rejects.toBeDefined();

    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('refresh-1');
});

test('a refresh retries with the token another tab saved in the meantime', async () => {
    sessionStore.start(authResult(1));
    fakeApi((request) => {
        const { refreshToken } = bodyOf(request);
        if (refreshToken === 'refresh-1') {
            // The other tab used refresh-1 a moment earlier, so the server already replaced it.
            window.localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh-from-other-tab');
            return { status: 401 };
        }
        return refreshToken === 'refresh-from-other-tab' ? { status: 200, data: authResult(3) } : { status: 401 };
    });

    await expect(refreshSession()).resolves.toMatchObject({ accessToken: 'access-3' });

    expect(sessionStore.getAccessToken()).toBe('access-3');
});

test('a failed sign-in is not followed by a refresh attempt', async () => {
    const requests = fakeApi(() => ({ status: 401, data: { message: 'Невірна пошта або пароль.' } }));

    await expect(http.post('auth/signin', { email: 'a@b.c', password: 'x' })).rejects.toBeDefined();

    expect(requests).toHaveLength(1);
});
