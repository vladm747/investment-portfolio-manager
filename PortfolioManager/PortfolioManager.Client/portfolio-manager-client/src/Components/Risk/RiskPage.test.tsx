import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import RiskPage from './RiskPage';
import { fakeApi, FakeReply } from '../../testing/fakeApi';
import { renderWithProviders } from '../../testing/renderWithProviders';
import { makeRiskForecast, stockFigures } from '../../testing/riskForecast';

const plain = (text: string | null) => (text ?? '').replace(/[  ]/g, ' ');

const serve = (forecast: () => FakeReply) =>
    fakeApi((request) => {
        if (request.url === 'portfolios/5') return { status: 200, data: { id: 5, name: 'Дивідендний' } };
        if (request.url === 'portfolios/5/forecast') return forecast();
        return { status: 404 };
    });

const renderPage = (route = '/portfolios/5/risk') => renderWithProviders(<RiskPage />, { route, path: '/portfolios/:id/risk' });

test('shows the portfolio, says it is a forecast, and names the portfolio in the title and the path', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));

    renderPage();

    expect(await screen.findByRole('heading', { name: 'Прогноз ризику на 3 місяці', level: 1 })).toBeInTheDocument();
    expect(await screen.findByText(/Це прогноз, а не гарантія/)).toBeInTheDocument();
    expect(plain((await screen.findByRole('region', { name: 'VaR 95% за день' })).textContent)).toContain('≈ 20,01 $');
    expect(await screen.findByText('Ризик кожної акції')).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Навігаційний ланцюжок' })).toHaveTextContent('ПортфеліДивіденднийПрогноз ризику');
    expect(document.title).toBe('Прогноз ризику · Дивідендний · Portfolio Manager');
});

test('the table has every figure of the portfolio and each stock', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));
    renderPage();

    const table = await screen.findByRole('table', { name: 'Прогноз ризику акцій у таблиці' });
    const row = (name: string) => plain((within(table).getByText(name).closest('tr') as HTMLElement).textContent);

    expect(row('Портфель')).toContain('100,0%');
    expect(row('Портфель')).toContain('19,1%13,7–25,2% · було 17,1%');
    expect(row('AAA')).toContain('60,0%');
    expect(row('AAA')).toContain('26,3%18,9–34,7% · було 23,8%');
    expect(row('BBB')).toContain('31,0%');
});

test('the chart compares one metric at a time on a shared axis, and every row reads out its figures', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));
    renderPage();

    expect(await screen.findByRole('img', { name: /^Волатильність \(річна\), AAA: прогноз 26,3%/ })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'VaR 95%' }));

    const figure = await screen.findByRole('img', { name: /^VaR 95% за день, AAA:/ });
    expect(plain(figure.getAttribute('aria-label'))).toBe(
        'VaR 95% за день, AAA: прогноз 2,65%, інтервал 1,60–3,38%, за останні 3 місяці 2,07%'
    );
    expect(screen.getByRole('button', { name: 'VaR 95%' })).toHaveAttribute('aria-pressed', 'true');
});

test('a stock without its own forecast and an unusual stock are explained', async () => {
    serve(() => ({
        status: 200,
        data: makeRiskForecast({
            assets: [
                { symbol: 'AAA', weight: 0.6, forecast: null },
                { symbol: 'GME', weight: 0.4, forecast: { ...stockFigures, outOfDomain: true, outOfDomainFeatures: ['rel_vol_63'] } },
            ],
        }),
    }));
    renderPage();

    expect(await screen.findByText('Не для всіх акцій є окремий прогноз')).toBeInTheDocument();
    expect(screen.getByText('Нетипова акція')).toBeInTheDocument();
    expect(screen.getAllByText('немає окремого прогнозу').length).toBeGreaterThan(0);
    expect(screen.getByLabelText('нетипово для моделі')).toBeInTheDocument();
});

test('the facts the forecast rests on', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));
    renderPage();

    const facts = plain((await screen.findByRole('region', { name: 'Про прогноз' })).textContent);

    expect(facts).toContain('risk-v1');
    expect(facts).toContain('Навчена на цінах до13.11.2026');
    expect(facts).toContain('3 місяці (63 торгові дні)');
    expect(facts).toContain('899 торгових днів');
    expect(facts).toContain('1 000,00 $');
});

test('a failed forecast can be tried again', async () => {
    let calls = 0;
    serve(() => (++calls === 1 ? { status: 502 } : { status: 200, data: makeRiskForecast() }));
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Спробувати ще раз' }));

    expect(await screen.findByText('Ризик кожної акції')).toBeInTheDocument();
});

test('an address that is not a number says the portfolio was not found', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));

    renderPage('/portfolios/abc/risk');

    expect(await screen.findByText('Портфель не знайдено')).toBeInTheDocument();
});
