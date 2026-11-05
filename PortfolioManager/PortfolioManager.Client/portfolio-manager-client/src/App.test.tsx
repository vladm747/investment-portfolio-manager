import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('a visitor who is not signed in is sent to the sign-in page', async () => {
  window.localStorage.clear();
  window.history.pushState({}, '', '/');

  render(<App />);

  expect(await screen.findByRole('heading', { name: 'Вхід' })).toBeInTheDocument();
  expect(window.location.pathname).toBe('/login');
  expect(screen.getByRole('link', { name: 'Реєстрація' })).toBeInTheDocument();
});
