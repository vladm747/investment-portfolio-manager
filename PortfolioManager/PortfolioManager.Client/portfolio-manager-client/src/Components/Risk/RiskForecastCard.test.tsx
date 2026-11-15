import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import RiskForecastCard from './RiskForecastCard';
import { fakeApi, FakeReply } from '../../testing/fakeApi';
import { renderWithProviders } from '../../testing/renderWithProviders';
import { makeRiskForecast, portfolioFigures } from '../../testing/riskForecast';

const plain = (text: string | null) => (text ?? '').replace(/[\u00a0\u202f]/g, ' ');

const serve = (forecast: () => FakeReply) =>
    fakeApi((request) => (request.url === 'portfolios/5/forecast' ? forecast() : { status: 404 }));

const renderCard = (hasPositions = true) =>
    renderWithProviders(<RiskForecastCard portfolioId={5} hasPositions={hasPositions} version={0} />);

const panel = async (name: string) => plain((await screen.findByRole('region', { name })).textContent);

test('shows the three forecasts with their interval, the last three months and the loss in money', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));

    renderCard();

    const volatility = await panel('Волатильність (річна)');
    expect(volatility).toContain('19,1%');
    expect(volatility).toContain('Інтервал: 13,7–25,2%');
    expect(volatility).toContain('За останні 3 місяці: 17,1%');
    expect(volatility).not.toContain('$');

    const valueAtRisk = await panel('VaR 95% за день');
    expect(valueAtRisk).toContain('2,00%');
    expect(valueAtRisk).toContain('≈ 20,01 $');
    expect(valueAtRisk).toContain('1,21–2,55% (12,10 $–25,49 $)');

    const drawdown = await panel('Максимальна просадка');
    expect(drawdown).toContain('13,3%');
    expect(drawdown).toContain('≈ 132,52 $');
    expect(drawdown).toContain('За останні 3 місяці: 5,9%');

    expect(plain(screen.getByText(/Прогноз моделі машинного навчання risk-v1/).textContent)).toContain('15.11.2026');
    expect(screen.getByRole('link', { name: 'Детальніше' })).toHaveAttribute('href', '/portfolios/5/risk');
});

test('each figure reads out what it draws', async () => {
    serve(() => ({ status: 200, data: makeRiskForecast() }));
    renderCard();

    const figure = await screen.findByRole('img', { name: /^Волатильність \(річна\):/ });

    expect(plain(figure.getAttribute('aria-label'))).toBe(
        'Волатильність (річна): прогноз 19,1%, інтервал 13,7–25,2%, за останні 3 місяці 17,1%'
    );
    // The legend tells the three marks apart by shape as well as colour.
    expect(screen.getByText('Прогноз')).toBeInTheDocument();
    expect(screen.getByText('Інтервал 80%')).toBeInTheDocument();
    expect(screen.getByText('Останні 3 місяці')).toBeInTheDocument();
});

test('an old model and an unusual portfolio are flagged above the figures', async () => {
    serve(() => ({
        status: 200,
        data: makeRiskForecast({
            model: { version: 'risk-v1', trainedThrough: '2026-11-13', horizonDays: 63, intervalLevel: 0.8, ageDays: 95, staleAfterDays: 60, stale: true },
            portfolio: { ...portfolioFigures, outOfDomain: true, outOfDomainFeatures: ['vol_252'] },
        }),
    }));

    renderCard();

    const alerts = await screen.findAllByRole('alert');
    expect(alerts.map((alert) => within(alert).getByText(/^(Модель потребує оновлення|Портфель нетиповий для моделі)$/).textContent)).toEqual([
        'Модель потребує оновлення',
        'Портфель нетиповий для моделі',
    ]);
    expect(plain(alerts[1].textContent)).toContain('волатильність за рік');
});

test('a refusal shows the API message, in Ukrainian, and can be tried again', async () => {
    let calls = 0;
    const message =
        'Для прогнозу ризику потрібна щонайменше річна історія цін кожної акції портфеля. CCC має ціни лише з 29.06.2026: 99 із 252 потрібних торгових днів.';
    serve(() => (++calls === 1 ? { status: 400, data: { message, errors: [message] } } : { status: 200, data: makeRiskForecast() }));

    renderCard();

    expect(await screen.findByText(message)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Спробувати ще раз' }));
    expect(await screen.findByRole('region', { name: 'Волатильність (річна)' })).toBeInTheDocument();
});

test('a failure of the server does not show its English details', async () => {
    serve(() => ({ status: 502, data: { message: 'Bad gateway at math.internal' } }));

    renderCard();

    expect(await screen.findByText('Сервіс розрахунків зараз не відповідає. Спробуйте ще раз за хвилину.')).toBeInTheDocument();
});

test('an empty portfolio asks for nothing and says when the forecast will appear', async () => {
    const requests = serve(() => ({ status: 200, data: makeRiskForecast() }));

    renderCard(false);

    expect(await screen.findByText('Прогноз з’явиться разом із позиціями')).toBeInTheDocument();
    expect(requests).toHaveLength(0);
});
