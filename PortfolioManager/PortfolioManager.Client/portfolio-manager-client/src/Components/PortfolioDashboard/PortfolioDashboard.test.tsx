import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import PortfolioDashboard from './PortfolioDashboard';
import { fakeApi } from '../../testing/fakeApi';
import { makeStock, renderWithProviders } from '../../testing/renderWithProviders';

const plain = (text: string | null) => (text ?? '').replace(/[  ]/g, ' ');
const pageText = () => plain(document.body.textContent);

const aapl = makeStock({ id: 1, symbol: 'AAPL', name: 'Apple Inc.', quantity: 2, entryPrice: 100, totalCurrentPrice: 260, gain: 60, gainPercentage: 30, portfolioId: 5 });
const msft = makeStock({ id: 2, symbol: 'MSFT', name: 'Microsoft Corp.', quantity: 1, entryPrice: 200, currentPrice: 300, totalCurrentPrice: 300, gain: 100, gainPercentage: 50, sector: 1, portfolioId: 5 });
const jnj = makeStock({ id: 3, symbol: 'JNJ', name: 'Johnson & Johnson', quantity: 1, entryPrice: 120, currentPrice: 100, totalCurrentPrice: 100, gain: -20, gainPercentage: -16.67, sector: 2, portfolioId: 5 });

const growth = { growthData: { '2024-03-12T00:00:00': 1000, '2024-03-13T00:00:00': 1100 } };

interface Options {
    stocks?: ReturnType<typeof makeStock>[];
    handle?: (url: string, method: string) => { status: number; data?: unknown } | undefined;
}

/** A fake server for portfolio 5; `handle` can answer a request differently before the defaults do. */
function serve({ stocks = [aapl, msft, jnj], handle }: Options = {}) {
    return fakeApi((request) => {
        const url = request.url ?? '';
        const method = request.method ?? 'get';
        const custom = handle?.(url, method);
        if (custom) return custom;
        if (url === 'portfolios/5') return { status: 200, data: { id: 5, name: 'Дивідендний' } };
        if (url === 'portfolios/5/positions') return { status: 200, data: stocks };
        if (url === 'portfolios/5/growth') return { status: 200, data: growth };
        return { status: 200, data: {} };
    });
}

const renderDashboard = () => renderWithProviders(<PortfolioDashboard />, { route: '/portfolios/5', path: '/portfolios/:id' });

const openRowMenu = async (symbol: string, item: RegExp) => {
    await userEvent.click(await screen.findByRole('button', { name: `Дії з позицією ${symbol}` }));
    await userEvent.click(await screen.findByRole('menuitem', { name: item }));
};

const symbolsInOrder = () =>
    screen
        .getAllByRole('row')
        .slice(1)
        .map((row) => within(row).queryByText(/^(AAPL|MSFT|JNJ)$/)?.textContent);

test('shows the portfolio, its totals and its positions', async () => {
    serve();

    renderDashboard();

    expect(await screen.findByRole('heading', { name: 'Дивідендний', level: 1 })).toBeInTheDocument();
    expect(await screen.findByText('AAPL')).toBeInTheDocument();
    // Invested 200 + 200 + 120 = 520, worth 260 + 300 + 100 = 660, gain 140.
    expect(pageText()).toContain('660,00 $');
    expect(pageText()).toContain('520,00 $');
    expect(pageText()).toContain('+140,00 $');
    expect(screen.getAllByText('Технології').length).toBeGreaterThan(0);
    await waitFor(() => expect(document.title).toBe('Дивідендний · Portfolio Manager'));
    expect(screen.getByRole('navigation', { name: 'Навігаційний ланцюжок' })).toHaveTextContent('Портфелі');
});

test('the growth chart has a period switch once the data has arrived', async () => {
    serve();
    renderDashboard();

    const group = await screen.findByRole('group', { name: 'Період' });

    expect(within(group).getAllByRole('button')).toHaveLength(5);
});

test('an empty portfolio says so, and offers nothing that needs positions', async () => {
    const requests = serve({ stocks: [] });
    renderDashboard();

    expect(await screen.findByText('У портфелі ще немає позицій')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Оптимізувати' })).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByRole('button', { name: 'Оновити ціни' })).toBeDisabled();
    // The math service would reject an empty portfolio, so the chart is not even requested.
    expect(requests.some((request) => request.url === 'portfolios/5/growth')).toBe(false);
});

test('positions are sorted by value at first and by any column on request', async () => {
    serve();
    renderDashboard();
    await screen.findByText('AAPL');

    expect(symbolsInOrder()).toEqual(['MSFT', 'AAPL', 'JNJ']);

    await userEvent.click(screen.getByRole('button', { name: /^Позиція/ }));
    expect(symbolsInOrder()).toEqual(['AAPL', 'JNJ', 'MSFT']);

    await userEvent.click(screen.getByRole('button', { name: /^Позиція/ }));
    expect(symbolsInOrder()).toEqual(['MSFT', 'JNJ', 'AAPL']);
});

describe('changing the quantity', () => {
    test('a bad number is refused', async () => {
        const requests = serve();
        renderDashboard();
        await openRowMenu('AAPL', /Змінити кількість/);

        const dialog = await screen.findByRole('dialog');
        const field = within(dialog).getByRole('textbox', { name: /Кількість акцій/ });
        await userEvent.clear(field);
        await userEvent.type(field, '0');
        await userEvent.click(within(dialog).getByRole('button', { name: 'Зберегти' }));

        expect(await within(dialog).findByText('Кількість має бути цілим числом, більшим за нуль.')).toBeInTheDocument();
        expect(requests.some((request) => request.method === 'put')).toBe(false);
    });

    test('a good number is saved and positions, totals and chart are loaded again', async () => {
        const requests = serve({ handle: (url, method) => (method === 'put' ? { status: 200, data: { ...aapl, quantity: 30 } } : undefined) });
        renderDashboard();
        await openRowMenu('AAPL', /Змінити кількість/);

        const dialog = await screen.findByRole('dialog');
        const field = within(dialog).getByRole('textbox', { name: /Кількість акцій/ });
        expect(field).toHaveValue('2');
        await userEvent.clear(field);
        await userEvent.type(field, '30');
        await userEvent.click(within(dialog).getByRole('button', { name: 'Зберегти' }));

        expect(await screen.findByText('Кількість AAPL змінено на 30')).toBeInTheDocument();
        const put = requests.find((request) => request.method === 'put' && request.url === 'positions/1');
        expect(put).toBeDefined();
        expect(JSON.parse(put!.data)).toEqual({ quantity: 30 });
        await waitFor(() => expect(requests.filter((request) => request.url === 'portfolios/5/positions')).toHaveLength(2));
        await waitFor(() => expect(requests.filter((request) => request.url === 'portfolios/5')).toHaveLength(2));
        await waitFor(() => expect(requests.filter((request) => request.url === 'portfolios/5/growth')).toHaveLength(2));
    });

    test('a refusal from the server stays in the dialog', async () => {
        serve({ handle: (url, method) => (method === 'put' ? { status: 400, data: 'Stock not updated' } : undefined) });
        renderDashboard();
        await openRowMenu('AAPL', /Змінити кількість/);

        const dialog = await screen.findByRole('dialog');
        const field = within(dialog).getByRole('textbox', { name: /Кількість акцій/ });
        await userEvent.clear(field);
        await userEvent.type(field, '5');
        await userEvent.click(within(dialog).getByRole('button', { name: 'Зберегти' }));

        expect(await within(dialog).findByRole('alert')).toBeInTheDocument();
    });
});

test('a position is deleted only after confirmation', async () => {
    const requests = serve({ handle: (url, method) => (method === 'delete' ? { status: 200, data: true } : undefined) });
    renderDashboard();
    await openRowMenu('JNJ', /Видалити/);

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText('Видалити позицію JNJ?')).toBeInTheDocument();
    expect(requests.some((request) => request.method === 'delete')).toBe(false);

    await userEvent.click(within(dialog).getByRole('button', { name: 'Видалити' }));

    expect(await screen.findByText('Позицію JNJ видалено')).toBeInTheDocument();
    expect(requests.some((request) => request.method === 'delete' && request.url === 'positions/3')).toBe(true);
});

describe('refreshing prices', () => {
    test('refreshes, reloads the positions and shows when it happened', async () => {
        const requests = serve();
        renderDashboard();

        await userEvent.click(await screen.findByRole('button', { name: 'Оновити ціни' }));

        expect(await screen.findByText('Ціни оновлено')).toBeInTheDocument();
        expect(requests.some((request) => request.method === 'post' && request.url === 'portfolios/5/prices/refresh')).toBe(true);
        expect(await screen.findByText(/Ціни оновлено сьогодні о/)).toBeInTheDocument();
        await waitFor(() => expect(requests.filter((request) => request.url === 'portfolios/5/positions')).toHaveLength(2));
    });

    test('a failure is reported and nothing else changes', async () => {
        serve({ handle: (url) => (url === 'portfolios/5/prices/refresh' ? { status: 500 } : undefined) });
        renderDashboard();

        await userEvent.click(await screen.findByRole('button', { name: 'Оновити ціни' }));

        expect(await screen.findByText(/Не вдалося оновити ціни/)).toBeInTheDocument();
    });
});

describe('adding a position', () => {
    const openDialog = async () => {
        await screen.findByText('AAPL');
        await userEvent.click(screen.getAllByRole('button', { name: 'Додати позицію' })[0]);
        return screen.findByRole('dialog');
    };
    const fill = async (dialog: HTMLElement, values: { symbol?: string; quantity?: string; price?: string; date?: string }) => {
        if (values.symbol !== undefined) await userEvent.type(within(dialog).getByRole('textbox', { name: /Тікер/ }), values.symbol);
        if (values.quantity !== undefined) await userEvent.type(within(dialog).getByRole('textbox', { name: /Кількість/ }), values.quantity);
        if (values.price !== undefined) await userEvent.type(within(dialog).getByRole('textbox', { name: /Середня ціна входу/ }), values.price);
        if (values.date !== undefined) {
            const date = within(dialog).getByLabelText(/Дата входу/) as HTMLInputElement;
            await userEvent.clear(date);
            await userEvent.type(date, values.date);
        }
    };

    test('every wrong field is explained and nothing is sent', async () => {
        const requests = serve();
        renderDashboard();

        const dialog = await openDialog();
        await userEvent.click(within(dialog).getByRole('button', { name: 'Додати' }));

        expect(await within(dialog).findByText(/Тікер: до 10 символів/)).toBeInTheDocument();
        expect(within(dialog).getByText('Кількість має бути цілим числом, більшим за нуль.')).toBeInTheDocument();
        expect(within(dialog).getByText('Вкажіть ціну входу більшу за нуль.')).toBeInTheDocument();
        expect(requests.some((request) => request.method === 'post')).toBe(false);
    });

    test('a good position is sent with an upper-case ticker, a decimal comma and a plain date', async () => {
        const requests = serve({ handle: (url, method) => (method === 'post' ? { status: 200, data: aapl } : undefined) });
        renderDashboard();

        const dialog = await openDialog();
        await fill(dialog, { symbol: 'nvda', quantity: '15', price: '172,40', date: '2024-03-12' });
        await userEvent.click(within(dialog).getByRole('button', { name: 'Додати' }));

        expect(await screen.findByText('Позицію NVDA додано')).toBeInTheDocument();
        const post = requests.find((request) => request.method === 'post')!;
        // The portfolio is in the address; the body holds only the position.
        expect(post.url).toBe('portfolios/5/positions');
        expect(JSON.parse(post.data)).toEqual({ symbol: 'NVDA', quantity: 15, entryPrice: 172.4, entryDate: '2024-03-12T00:00:00' });
        await waitFor(() => expect(requests.filter((request) => request.method === 'get' && request.url === 'portfolios/5/positions')).toHaveLength(2));
    });

    test('when the server refuses the ticker the message appears in the dialog', async () => {
        serve({ handle: (url, method) => (method === 'post' ? { status: 500, data: { message: 'Stock symbol is invalid' } } : undefined) });
        renderDashboard();

        const dialog = await openDialog();
        await fill(dialog, { symbol: 'ZZZZ', quantity: '1', price: '10', date: '2024-03-12' });
        await userEvent.click(within(dialog).getByRole('button', { name: 'Додати' }));

        expect(await within(dialog).findByRole('alert')).toHaveTextContent('Не вдалося додати позицію');
    });
});
