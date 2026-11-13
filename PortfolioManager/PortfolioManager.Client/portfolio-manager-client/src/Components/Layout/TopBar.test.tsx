import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../../Auth/AuthProvider';
import { sessionStore } from '../../Auth/session';
import { ThemeModeProvider } from '../../Theme/ThemeModeProvider';
import { adminUser, authResult } from '../../testing/fakeApi';
import TopBar from './TopBar';

const renderTopBar = (path = '/') =>
    render(
        <ThemeModeProvider>
            <AuthProvider>
                <MemoryRouter initialEntries={[path]}>
                    <TopBar />
                </MemoryRouter>
            </AuthProvider>
        </ThemeModeProvider>
    );

beforeEach(() => {
    sessionStore.end();
    window.localStorage.clear();
});

test('an administrator sees the «Користувачі» link next to «Портфелі»', () => {
    sessionStore.start(authResult(1, adminUser));

    renderTopBar();

    expect(screen.getByRole('link', { name: 'Користувачі' })).toHaveAttribute('href', '/admin/users');
    expect(screen.getByRole('link', { name: 'Портфелі' })).toBeInTheDocument();
});

test('the «Користувачі» link is marked as the current page on the admin pages only', () => {
    sessionStore.start(authResult(1, adminUser));

    renderTopBar('/admin/users');

    expect(screen.getByRole('link', { name: 'Користувачі' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('link', { name: 'Портфелі' })).not.toHaveAttribute('aria-current');
});

test('an investor does not see the «Користувачі» link', () => {
    sessionStore.start(authResult(1));

    renderTopBar();

    expect(screen.getByRole('link', { name: 'Портфелі' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Користувачі' })).not.toBeInTheDocument();
});

test('a visitor who is not signed in sees only «Увійти»: no sections and no sign-up button', () => {
    renderTopBar();

    expect(screen.getByRole('link', { name: 'Увійти' })).toHaveAttribute('href', '/login');
    expect(screen.queryByRole('link', { name: 'Реєстрація' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Користувачі' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Портфелі' })).not.toBeInTheDocument();
});
