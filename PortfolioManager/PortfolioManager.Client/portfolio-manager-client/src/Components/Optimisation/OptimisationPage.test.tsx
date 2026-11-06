import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import OptimisationPage from './OptimisationPage';
import { fakeApi } from '../../testing/fakeApi';
import { renderWithProviders } from '../../testing/renderWithProviders';

const plain = (text: string | null) => (text ?? '').replace(/[  ]/g, ' ');

const comparison = (metricName: string, strategyBaseValue: number, strategyOptimizedValue: number) => ({ metricName, strategyBaseValue, strategyOptimizedValue });

// Eight of nine metrics improve; the maximum drawdown gets a little deeper.
const result = {
    comparisons: [
        comparison('Загальний дохід', 2.96, 3.84),
        comparison('Середньорічний темп приросту (CAGR﹪)', 0.237, 0.274),
        comparison('Коефіцієнт Шарпа', 1.18, 1.46),
        comparison('Коефіцієнт Сортіно', 1.72, 2.19),
        comparison('Коефіцієнт Кальмара', 0.83, 0.93),
        comparison('Максимальна просадка', -0.2847, -0.2961),
        comparison('Показник волатильності (річний)', 0.2196, 0.1904),
        comparison('Прогнозований місячний дохід', 0.0198, 0.0228),
        comparison('Прогнозований річний дохід', 0.237, 0.274),
    ],
    weights: { baseWeights: [0.6, 0.4], optimizedWeights: { AAPL: 0.9, MSFT: 0.1 } },
};

const serve = (optimisation: () => { status: number; data?: unknown }) =>
    fakeApi((request) => {
        if (request.url === 'portfolio/5') return { status: 200, data: { id: 5, name: 'Дивідендний' } };
        if (request.url === 'statistics/opt/5') return optimisation();
        return { status: 404 };
    });

const renderPage = (route = '/portfolios/5/optimisation') =>
    renderWithProviders(<OptimisationPage />, { route, path: '/portfolios/:id/optimisation' });

test('compares the current and the optimised portfolio and notes that the result is in-sample', async () => {
    serve(() => ({ status: 200, data: result }));

    renderPage();

    expect(await screen.findByRole('heading', { name: 'Оптимізація портфеля', level: 1 })).toBeInTheDocument();
    expect(await screen.findByText(/in-sample/)).toBeInTheDocument();
    expect(screen.getByText('Оптимізований портфель кращий за 8 з 9 показників.')).toBeInTheDocument();
    expect(document.title).toBe('Оптимізація · Дивідендний · Portfolio Manager');
    expect(screen.getByRole('navigation', { name: 'Навігаційний ланцюжок' })).toHaveTextContent('ПортфеліДивіденднийОптимізація');
});

test('percentages and ratios are formatted by what they are', async () => {
    serve(() => ({ status: 200, data: result }));
    renderPage();

    const table = await screen.findByRole('table', { name: 'Порівняння показників' });
    const row = (name: string) => within(table).getByText(name).closest('tr') as HTMLElement;

    expect(plain(row('Загальний дохід').textContent)).toContain('296,00%');
    expect(plain(row('Загальний дохід').textContent)).toContain('384,00%');
    expect(plain(row('Коефіцієнт Шарпа').textContent)).toContain('1,18');
    expect(plain(row('Коефіцієнт Шарпа').textContent)).toContain('1,46');
    expect(plain(row('Максимальна просадка').textContent)).toContain('−28,47%');
});

test('each metric says whether the optimised value is better or worse by its own rule', async () => {
    serve(() => ({ status: 200, data: result }));
    renderPage();

    const table = await screen.findByRole('table', { name: 'Порівняння показників' });
    const verdictOf = (name: string) => within(within(table).getByText(name).closest('tr') as HTMLElement).getByText(/^(краще|гірше|без змін)$/).textContent;

    expect(verdictOf('Коефіцієнт Шарпа')).toBe('краще');
    // Lower volatility is better.
    expect(verdictOf('Показник волатильності (річний)')).toBe('краще');
    // A deeper drawdown is worse even though its number is smaller.
    expect(verdictOf('Максимальна просадка')).toBe('гірше');
});

test('every metric has an explanation behind its info button', async () => {
    serve(() => ({ status: 200, data: result }));
    renderPage();

    await screen.findByRole('table', { name: 'Порівняння показників' });

    expect(screen.getAllByRole('button', { name: /^Що означає/ })).toHaveLength(9);
    expect(screen.getByRole('button', { name: 'Що означає «Коефіцієнт Шарпа»' })).toBeInTheDocument();
});

test('weights are listed per ticker in percent', async () => {
    serve(() => ({ status: 200, data: result }));
    renderPage();

    const table = await screen.findByRole('table', { name: 'Ваги акцій у таблиці' });

    expect(plain(within(table).getByText('AAPL').closest('tr')!.textContent)).toContain('60,0%');
    expect(plain(within(table).getByText('AAPL').closest('tr')!.textContent)).toContain('90,0%');
    expect(plain(within(table).getByText('MSFT').closest('tr')!.textContent)).toContain('+30,0 п.п.'.replace('+30', '−30'));
});

test('a failed calculation can be tried again', async () => {
    let calls = 0;
    serve(() => (++calls === 1 ? { status: 500 } : { status: 200, data: result }));
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Спробувати ще раз' }));

    expect(await screen.findByText(/in-sample/)).toBeInTheDocument();
});

test('an address that is not a number says the portfolio was not found', async () => {
    serve(() => ({ status: 200, data: result }));

    renderPage('/portfolios/abc/optimisation');

    expect(await screen.findByText('Портфель не знайдено')).toBeInTheDocument();
});
