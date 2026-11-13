import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';
import { sessionStore } from './Auth/session';
import { adminUser, authResult, fakeApi } from './testing/fakeApi';

beforeEach(() => {
  sessionStore.end();
  window.localStorage.clear();
});

test('a visitor who is not signed in is sent to the sign-in page', async () => {
  window.history.pushState({}, '', '/');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Вхід' })).toBeInTheDocument();
  expect(window.location.pathname).toBe('/login');
});

test('there is no sign-up page any more: the old address is a page that does not exist', async () => {
  window.history.pushState({}, '', '/sign-up');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Сторінку не знайдено' })).toBeInTheDocument();
  expect(screen.queryByRole('link', { name: 'Реєстрація' })).not.toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Увійти' })).toBeInTheDocument();
});

test('the privacy policy is public: a visitor who is not signed in can read it', async () => {
  window.history.pushState({}, '', '/privacy');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Політика конфіденційності', level: 1 })).toBeInTheDocument();
  expect(window.location.pathname).toBe('/privacy');
  expect(screen.getByText(/Пароля в нас немає/)).toBeInTheDocument();
  expect(document.title).toBe('Політика конфіденційності · Portfolio Manager');
});

test('an administrator opens the user dashboard at /admin/users', async () => {
  sessionStore.start(authResult(1, adminUser));
  fakeApi((request) =>
    request.url === 'admin/users'
      ? { status: 200, data: { stats: { totalUsers: 1, admins: 1, activeLast7Days: 1, totalPortfolios: 0 }, users: [{ id: 'admin-1', fullName: 'Адмін Тестовий', email: 'admin@example.com', roles: ['investor', 'admin'], portfolioCount: 0, lastActiveAt: '2026-11-12T10:00:00Z' }] } }
      : { status: 404 }
  );
  window.history.pushState({}, '', '/admin/users');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Користувачі', level: 1 })).toBeInTheDocument();
  expect(await screen.findByText('admin@example.com')).toBeInTheDocument();
  expect(window.location.pathname).toBe('/admin/users');
});

test('an investor who types the admin address is sent to the portfolio list and never asks the API', async () => {
  sessionStore.start(authResult(1));
  const requests = fakeApi(() => ({ status: 200, data: [] }));
  window.history.pushState({}, '', '/admin/users');

  render(<App />);

  expect(await screen.findByText('Ще немає портфелів')).toBeInTheDocument();
  expect(window.location.pathname).toBe('/');
  expect(requests.map((request) => request.url)).not.toContain('admin/users');
});

test('a visitor who is not signed in cannot open the admin address either', async () => {
  window.history.pushState({}, '', '/admin/users');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Вхід' })).toBeInTheDocument();
});

const position = {
  id: 1, symbol: 'AAPL', name: 'Apple Inc.', quantity: 2, entryPrice: 100, currentPrice: 130,
  totalCurrentPrice: 260, gain: 60, gainPercentage: 30, entryDate: '2024-03-12T00:00:00', currency: 'USD',
  sector: 0, portfolioId: 5,
};

test('a portfolio opens by the id in the address, so a reload shows the same portfolio', async () => {
  sessionStore.start(authResult(1));
  const requests = fakeApi((request) => {
    if (request.url === 'portfolios/5') return { status: 200, data: { id: 5, name: 'Дивідендний' } };
    if (request.url === 'portfolios/5/positions') return { status: 200, data: [position] };
    if (request.url === 'portfolios/5/growth') return { status: 200, data: { growthData: {} } };
    return { status: 404 };
  });
  window.history.pushState({}, '', '/portfolios/5');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Дивідендний', level: 1 })).toBeInTheDocument();
  expect((await screen.findAllByText('AAPL')).length).toBeGreaterThan(0);
  expect(requests.map((request) => request.url)).toEqual(expect.arrayContaining(['portfolios/5', 'portfolios/5/positions']));
});

test('an address of a portfolio that does not exist says so instead of crashing', async () => {
  sessionStore.start(authResult(1));
  fakeApi(() => ({ status: 404 }));
  window.history.pushState({}, '', '/portfolios/999');

  render(<App />);

  expect(await screen.findByText('Портфель не знайдено')).toBeInTheDocument();
});

test('an address that is not a number says the portfolio was not found', async () => {
  sessionStore.start(authResult(1));
  const requests = fakeApi(() => ({ status: 500 }));
  window.history.pushState({}, '', '/portfolios/abc');

  render(<App />);

  expect(await screen.findByText('Портфель не знайдено')).toBeInTheDocument();
  expect(requests.filter((request) => request.url?.startsWith('portfolios/NaN'))).toHaveLength(0);
});
