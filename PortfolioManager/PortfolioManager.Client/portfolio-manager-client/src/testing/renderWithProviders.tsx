import React from 'react';
import { render } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { NotificationsProvider } from '../Components/Shared/Notifications';
import { ThemeModeProvider } from '../Theme/ThemeModeProvider';
import StockDto from '../DTO/StockDto';

function LocationProbe() {
    const location = useLocation();
    return <output data-testid="location">{location.pathname}</output>;
}

/** Renders a page the way the app does (theme, notifications, router) at the given address. */
export function renderWithProviders(ui: React.ReactElement, { route = '/', path = '*' }: { route?: string; path?: string } = {}) {
    return render(
        <ThemeModeProvider>
            <NotificationsProvider>
                <MemoryRouter initialEntries={[route]}>
                    <Routes>
                        <Route path={path} element={ui} />
                    </Routes>
                    <LocationProbe />
                </MemoryRouter>
            </NotificationsProvider>
        </ThemeModeProvider>
    );
}

export const makeStock = (overrides: Partial<StockDto> = {}): StockDto => ({
    id: 1,
    symbol: 'AAPL',
    name: 'Apple Inc.',
    quantity: 2,
    entryPrice: 100,
    currentPrice: 130,
    totalCurrentPrice: 260,
    gain: 60,
    gainPercentage: 30,
    entryDate: '2024-03-12T00:00:00' as unknown as Date,
    currency: 'USD',
    sector: 0,
    portfolioId: 1,
    ...overrides,
});

/** Makes `window.matchMedia` report that the system prefers a dark (or light) colour scheme. */
export function setSystemColorScheme(dark: boolean) {
    Object.defineProperty(window, 'matchMedia', {
        writable: true,
        configurable: true,
        value: (query: string) => ({
            matches: query.includes('dark') ? dark : false,
            media: query,
            addEventListener: () => undefined,
            removeEventListener: () => undefined,
            addListener: () => undefined,
            removeListener: () => undefined,
            dispatchEvent: () => false,
            onchange: null,
        }),
    });
}
