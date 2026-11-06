import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import PortfoliosPage from './PortfoliosPage';
import { fakeApi } from '../../testing/fakeApi';
import { makeStock, renderWithProviders } from '../../testing/renderWithProviders';

const plain = (text: string | null) => (text ?? '').replace(/[  ]/g, ' ');
const pageText = () => plain(document.body.textContent);

const portfolio = (id: number, name: string, stocks = [] as ReturnType<typeof makeStock>[]) => ({
    id, name, totalEntryPrice: 0, totalCurrentPrice: 0, totalGain: 0, totalGainPercentage: 0, userId: 'u1', stocks,
});

// 200 invested and worth 260; 500 invested and worth 450.
const filled = portfolio(1, 'Довгострокові', [
    makeStock({ id: 1, symbol: 'AAPL', entryPrice: 100, quantity: 2, totalCurrentPrice: 260 }),
    makeStock({ id: 2, symbol: 'MSFT', entryPrice: 50, quantity: 10, totalCurrentPrice: 450 }),
]);
const empty = portfolio(2, 'Порожній');

const renderPage = () => renderWithProviders(<PortfoliosPage />);

test('shows the totals across portfolios and one row per portfolio', async () => {
    fakeApi(() => ({ status: 200, data: [filled, empty] }));

    renderPage();

    expect(await screen.findByRole('link', { name: 'Довгострокові' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Порожній' })).toBeInTheDocument();
    expect(pageText()).toContain('2 портфелі · 2 позиції');
    // Invested 700, worth 710, gain 10.
    expect(pageText()).toContain('710,00 $');
    expect(pageText()).toContain('700,00 $');
    expect(pageText()).toContain('+10,00 $');
    expect(document.title).toBe('Портфелі · Portfolio Manager');
});

test('a click anywhere on a row opens the portfolio', async () => {
    fakeApi(() => ({ status: 200, data: [filled, empty] }));
    renderPage();
    await screen.findByRole('link', { name: 'Порожній' });

    await userEvent.click(screen.getByText('Немає позицій'));

    expect(screen.getByTestId('location')).toHaveTextContent('/portfolios/2');
});

test('shows an empty state when there are no portfolios', async () => {
    fakeApi(() => ({ status: 200, data: [] }));

    renderPage();

    expect(await screen.findByText('Ще немає портфелів')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Створити портфель' }).length).toBeGreaterThan(0);
});

test('a failed load offers to try again', async () => {
    let calls = 0;
    fakeApi(() => (++calls === 1 ? { status: 500 } : { status: 200, data: [filled] }));

    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Спробувати ще раз' }));

    expect(await screen.findByRole('link', { name: 'Довгострокові' })).toBeInTheDocument();
});

describe('deleting a portfolio', () => {
    const openDeleteDialog = async () => {
        await userEvent.click(await screen.findByRole('button', { name: 'Дії з портфелем «Порожній»' }));
        await userEvent.click(await screen.findByRole('menuitem', { name: /Видалити/ }));
        return screen.findByRole('dialog');
    };

    test('asks first and deletes only after confirmation', async () => {
        const requests = fakeApi((request) =>
            request.method === 'delete' ? { status: 200, data: 1 } : { status: 200, data: [filled, empty] }
        );
        renderPage();

        const dialog = await openDeleteDialog();
        expect(within(dialog).getByText('Видалити портфель «Порожній»?')).toBeInTheDocument();
        expect(requests.some((request) => request.method === 'delete')).toBe(false);

        await userEvent.click(within(dialog).getByRole('button', { name: 'Видалити' }));

        await waitFor(() => expect(requests.some((request) => request.method === 'delete' && request.url === 'portfolio/2')).toBe(true));
        expect(await screen.findByText('Портфель «Порожній» видалено')).toBeInTheDocument();
        // The list is loaded again.
        await waitFor(() => expect(requests.filter((request) => request.method === 'get')).toHaveLength(2));
    });

    test('cancelling keeps the portfolio', async () => {
        const requests = fakeApi(() => ({ status: 200, data: [filled, empty] }));
        renderPage();

        const dialog = await openDeleteDialog();
        await userEvent.click(within(dialog).getByRole('button', { name: 'Скасувати' }));

        await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
        expect(requests.some((request) => request.method === 'delete')).toBe(false);
    });

    test('a failure is reported and the dialog stays open for another try', async () => {
        fakeApi((request) => (request.method === 'delete' ? { status: 500 } : { status: 200, data: [filled, empty] }));
        renderPage();

        const dialog = await openDeleteDialog();
        await userEvent.click(within(dialog).getByRole('button', { name: 'Видалити' }));

        expect(await screen.findByText('Не вдалося видалити портфель. Спробуйте ще раз.')).toBeInTheDocument();
    });
});

describe('creating a portfolio', () => {
    const openDialog = async () => {
        await screen.findByRole('link', { name: 'Довгострокові' });
        await userEvent.click(screen.getByRole('button', { name: 'Створити портфель' }));
        return screen.findByRole('dialog');
    };

    test('an empty name is refused with a message and nothing is sent', async () => {
        const requests = fakeApi(() => ({ status: 200, data: [filled] }));
        renderPage();

        const dialog = await openDialog();
        await userEvent.click(within(dialog).getByRole('button', { name: 'Створити' }));

        expect(await within(dialog).findByText('Вкажіть назву портфеля.')).toBeInTheDocument();
        expect(requests.some((request) => request.method === 'post')).toBe(false);
    });

    test('a name over 100 characters is refused', async () => {
        fakeApi(() => ({ status: 200, data: [filled] }));
        renderPage();

        const dialog = await openDialog();
        await userEvent.type(within(dialog).getByRole('textbox', { name: /Назва портфеля/ }), 'а'.repeat(101));
        await userEvent.click(within(dialog).getByRole('button', { name: 'Створити' }));

        expect(await within(dialog).findByText('Назва має бути не довшою за 100 символів.')).toBeInTheDocument();
    });

    test('a good name creates the portfolio and the list is loaded again', async () => {
        const requests = fakeApi((request) =>
            request.method === 'post' ? { status: 200, data: portfolio(3, 'Нова') } : { status: 200, data: [filled] }
        );
        renderPage();

        const dialog = await openDialog();
        await userEvent.type(within(dialog).getByRole('textbox', { name: /Назва портфеля/ }), '  Нова  ');
        await userEvent.click(within(dialog).getByRole('button', { name: 'Створити' }));

        expect(await screen.findByText('Портфель «Нова» створено')).toBeInTheDocument();
        const post = requests.find((request) => request.method === 'post')!;
        expect(JSON.parse(post.data)).toEqual({ name: 'Нова' });
        await waitFor(() => expect(requests.filter((request) => request.method === 'get')).toHaveLength(2));
    });

    test('an error from the server is shown inside the dialog', async () => {
        fakeApi((request) =>
            request.method === 'post' ? { status: 400, data: { message: 'Така назва вже є.' } } : { status: 200, data: [filled] }
        );
        renderPage();

        const dialog = await openDialog();
        await userEvent.type(within(dialog).getByRole('textbox', { name: /Назва портфеля/ }), 'Нова');
        await userEvent.click(within(dialog).getByRole('button', { name: 'Створити' }));

        expect(await within(dialog).findByRole('alert')).toHaveTextContent('Така назва вже є.');
    });
});
