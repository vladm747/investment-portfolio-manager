import { render, screen, waitFor } from '@testing-library/react';
import App from '../../App';
import { REFRESH_TOKEN_KEY, sessionStore } from '../../Auth/session';
import { authResult, bodyOf, fakeApi, installFakeGoogle, removeFakeGoogle } from '../../testing/fakeApi';

beforeEach(() => {
    sessionStore.end();
    window.localStorage.clear();
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', 'test-client.apps.googleusercontent.com');
    window.history.pushState({}, '', '/login');
});

afterEach(() => {
    removeFakeGoogle();
    vi.unstubAllEnvs();
});

/** The page is open and Google's button has been drawn. */
async function openSignIn() {
    const google = installFakeGoogle();
    render(<App />);
    await screen.findByRole('heading', { name: 'Вхід' });
    await waitFor(() => expect(google.buttons.length).toBeGreaterThan(0));
    return google;
}

test('the sign-in page offers Google and nothing else: no password, no sign-up', async () => {
    await openSignIn();

    expect(screen.queryByLabelText(/Пароль/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Пошта/)).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Зареєструватися/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Реєстрація/ })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'політика конфіденційності' })).toHaveAttribute('href', '/privacy');
    expect(document.title).toBe('Вхід · Portfolio Manager');
});

test('the token from Google is sent to the API, the session starts, and the portfolio list opens', async () => {
    const requests = fakeApi((request) => (request.url === 'auth/google' ? { status: 200, data: authResult(1) } : { status: 200, data: [] }));
    const google = await openSignIn();

    google.chooseAccount('google-id-token');

    expect(await screen.findByText('Ще немає портфелів')).toBeInTheDocument();
    const signIn = requests.find((request) => request.url === 'auth/google')!;
    expect(signIn.method).toBe('post');
    expect(bodyOf(signIn)).toEqual({ credential: 'google-id-token' });
    expect(signIn.withCredentials).toBe(true);
    expect(sessionStore.getAccessToken()).toBe('access-1');
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('refresh-1');
    expect(window.location.pathname).toBe('/');
});

test('a user sent to sign in from a page lands on that page afterwards', async () => {
    fakeApi((request) => {
        if (request.url === 'auth/google') return { status: 200, data: authResult(1) };
        if (request.url === 'portfolios/5') return { status: 200, data: { id: 5, name: 'Дивідендний' } };
        if (request.url === 'portfolios/5/growth') return { status: 200, data: { growthData: {} } };
        return { status: 200, data: [] };
    });
    window.history.pushState({}, '', '/portfolios/5');
    const google = installFakeGoogle();
    render(<App />);
    await waitFor(() => expect(google.buttons.length).toBeGreaterThan(0));

    google.chooseAccount('google-id-token');

    expect(await screen.findByRole('heading', { name: 'Дивідендний', level: 1 })).toBeInTheDocument();
    expect(window.location.pathname).toBe('/portfolios/5');
});

test('a refused token shows the API message, starts no session and tries no refresh', async () => {
    const requests = fakeApi(() => ({ status: 401, data: { message: 'Не вдалося підтвердити вхід через Google. Спробуйте ще раз.' } }));
    const google = await openSignIn();

    google.chooseAccount('tampered');

    expect(await screen.findByRole('alert')).toHaveTextContent('Не вдалося підтвердити вхід через Google. Спробуйте ще раз.');
    expect(requests.map((request) => request.url)).toEqual(['auth/google']);
    expect(sessionStore.get()).toBeNull();
    expect(window.location.pathname).toBe('/login');
});

test('a server error shows a plain message, not the server text, and the user can try again', async () => {
    let calls = 0;
    const requests = fakeApi(() => {
        calls += 1;
        return calls === 1
            ? { status: 500, data: { message: 'System.InvalidOperationException at prod-sql' } }
            : { status: 200, data: authResult(1) };
    });
    const google = await openSignIn();

    google.chooseAccount('token');
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Не вдалося увійти. Спробуйте ще раз.');
    expect(alert).not.toHaveTextContent('prod-sql');

    google.chooseAccount('token-again');

    await waitFor(() => expect(sessionStore.get()).not.toBeNull());
    expect(requests.filter((request) => request.url === 'auth/google')).toHaveLength(2);
});

test('with the API unreachable the user is told to check the connection', async () => {
    fakeApi(() => 'network-error');
    const google = await openSignIn();

    google.chooseAccount('token');

    expect(await screen.findByRole('alert')).toHaveTextContent('Не вдалося з\'єднатися з сервером');
});
