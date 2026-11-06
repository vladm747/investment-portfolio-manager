import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';
import { sessionStore } from './Auth/session';
import { authResult, fakeApi } from './testing/fakeApi';

beforeEach(() => {
  sessionStore.end();
  window.localStorage.clear();
});

test('a visitor who is not signed in is sent to the sign-in page', async () => {
  window.history.pushState({}, '', '/');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Вхід' })).toBeInTheDocument();
  expect(window.location.pathname).toBe('/login');
  expect(screen.getByRole('link', { name: 'Зареєструватися' })).toBeInTheDocument();
});

const position = {
  id: 1, symbol: 'AAPL', name: 'Apple Inc.', quantity: 2, entryPrice: 100, currentPrice: 130,
  totalCurrentPrice: 260, gain: 60, gainPercentage: 30, entryDate: '2024-03-12T00:00:00', currency: 'USD',
  sector: 0, portfolioId: 5,
};

test('a portfolio opens by the id in the address, so a reload shows the same portfolio', async () => {
  sessionStore.start(authResult(1));
  const requests = fakeApi((request) => {
    if (request.url === 'portfolio/5') return { status: 200, data: { id: 5, name: 'Дивідендний' } };
    if (request.url === 'stock/5') return { status: 200, data: [position] };
    if (request.url === 'statistics/growth/5') return { status: 200, data: { growthData: {} } };
    return { status: 404 };
  });
  window.history.pushState({}, '', '/portfolios/5');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Дивідендний', level: 1 })).toBeInTheDocument();
  expect((await screen.findAllByText('AAPL')).length).toBeGreaterThan(0);
  expect(requests.map((request) => request.url)).toEqual(expect.arrayContaining(['portfolio/5', 'stock/5']));
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
  expect(requests.filter((request) => request.url?.startsWith('portfolio/NaN'))).toHaveLength(0);
});
