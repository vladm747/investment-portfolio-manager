import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './AuthProvider';
import { GuestOnly, RequireAuth, RequireRole } from './RouteGuards';
import { REFRESH_TOKEN_KEY, sessionStore } from './session';
import { adminUser, authResult, bodyOf, fakeApi, testUser } from '../testing/fakeApi';

function SignOutButton() {
    const { signOut } = useAuth();
    return <button onClick={signOut}>Вийти</button>;
}

function renderApp(path: string) {
    return render(
        <AuthProvider>
            <MemoryRouter initialEntries={[path]}>
                <Routes>
                    <Route element={<RequireAuth />}>
                        <Route path="/" element={<><p>Мої портфелі</p><SignOutButton /></>} />
                        <Route element={<RequireRole role="admin" />}>
                            <Route path="/admin" element={<p>Панель адміністратора</p>} />
                        </Route>
                    </Route>
                    <Route element={<GuestOnly />}>
                        <Route path="/login" element={<p>Сторінка входу</p>} />
                    </Route>
                </Routes>
            </MemoryRouter>
        </AuthProvider>
    );
}

beforeEach(() => {
    sessionStore.end();
    window.localStorage.clear();
});

test('protected pages send an anonymous visitor to sign in', async () => {
    fakeApi(() => ({ status: 500 }));

    renderApp('/');

    expect(await screen.findByText('Сторінка входу')).toBeInTheDocument();
});

test('a session saved by an earlier visit is restored after a reload', async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh-1');
    const requests = fakeApi(() => ({ status: 200, data: authResult(2) }));

    renderApp('/');

    expect(await screen.findByText('Мої портфелі')).toBeInTheDocument();
    expect(bodyOf(requests[0])).toEqual({ refreshToken: 'refresh-1' });
    expect(sessionStore.get()?.user).toEqual(testUser);
});

test('a signed-in user who opens the sign-in page is sent back to the app', async () => {
    sessionStore.start(authResult(1));

    renderApp('/login');

    expect(await screen.findByText('Мої портфелі')).toBeInTheDocument();
});

test('signing out clears the session and revokes the refresh token on the server', async () => {
    sessionStore.start(authResult(1));
    const requests = fakeApi(() => ({ status: 204 }));

    renderApp('/');
    await userEvent.click(await screen.findByRole('button', { name: 'Вийти' }));

    expect(await screen.findByText('Сторінка входу')).toBeInTheDocument();
    expect(window.localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
    const signOut = requests.find((request) => request.url === 'auth/sign-out')!;
    expect(bodyOf(signOut)).toEqual({ refreshToken: 'refresh-1' });
    expect(signOut.headers.get('Authorization')).toBe('Bearer access-1');
});

test('a page for admins opens for a user who has the admin role', async () => {
    sessionStore.start(authResult(1, adminUser));

    renderApp('/admin');

    expect(await screen.findByText('Панель адміністратора')).toBeInTheDocument();
});

test('an investor who opens a page for admins is sent to the portfolio list', async () => {
    sessionStore.start(authResult(1));

    renderApp('/admin');

    expect(await screen.findByText('Мої портфелі')).toBeInTheDocument();
    expect(screen.queryByText('Панель адміністратора')).not.toBeInTheDocument();
});

test('a user with no roles at all does not pass a role guard', async () => {
    sessionStore.start(authResult(1, { ...testUser, roles: [] }));

    renderApp('/admin');

    expect(await screen.findByText('Мої портфелі')).toBeInTheDocument();
});

test('the role is read from the restored session: an admin who reloads the page is still let in', async () => {
    window.localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh-1');
    fakeApi(() => ({ status: 200, data: authResult(2, adminUser) }));

    renderApp('/admin');

    expect(await screen.findByText('Панель адміністратора')).toBeInTheDocument();
});

test('a visitor who is not signed in is sent to sign in, not to the list, from a page for admins', async () => {
    fakeApi(() => ({ status: 500 }));

    renderApp('/admin');

    expect(await screen.findByText('Сторінка входу')).toBeInTheDocument();
});
