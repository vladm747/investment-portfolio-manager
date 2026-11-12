import { bodyOf, fakeApi } from '../testing/fakeApi';
import { CreatePortfolio, DeletePortfolio, GetAllPortfoliosAsync, GetPortfolioAsync } from './PortfolioService';
import { GetGrowthStatsData, GetOptimisationData } from './StatisticService';
import { CreateStock, DeleteStock, GetAllStocksAsync, RefreshPrices, UpdateStockQuantity } from './StockService';

/**
 * The routes the API serves (PortfolioManager.API Controllers, Tests/Api/RestRoutesApiTests.cs). Each service
 * call is checked here against the method, address and body the API expects, so a rename on either side fails a test.
 */
const send = async (call: () => Promise<unknown>) => {
    const requests = fakeApi(() => ({ status: 200, data: [] }));
    await call();
    expect(requests).toHaveLength(1);
    return requests[0];
};

describe('portfolios', () => {
    test('list', async () => {
        const request = await send(() => GetAllPortfoliosAsync());
        expect([request.method, request.url]).toEqual(['get', 'portfolios']);
    });

    test('one', async () => {
        const request = await send(() => GetPortfolioAsync(5));
        expect([request.method, request.url]).toEqual(['get', 'portfolios/5']);
    });

    test('create posts the name to the collection', async () => {
        const request = await send(() => CreatePortfolio({ name: 'Основний' }));
        expect([request.method, request.url]).toEqual(['post', 'portfolios']);
        expect(bodyOf(request)).toEqual({ name: 'Основний' });
    });

    test('delete', async () => {
        const request = await send(() => DeletePortfolio(5));
        expect([request.method, request.url]).toEqual(['delete', 'portfolios/5']);
    });
});

describe('positions', () => {
    test('the positions of a portfolio', async () => {
        const request = await send(() => GetAllStocksAsync(5));
        expect([request.method, request.url]).toEqual(['get', 'portfolios/5/positions']);
    });

    test('a new position goes to its portfolio, and the portfolio is not in the body', async () => {
        const request = await send(() =>
            CreateStock(5, { symbol: 'AAPL', quantity: 2, entryPrice: 150.5, entryDate: '2024-05-06T00:00:00' })
        );
        expect([request.method, request.url]).toEqual(['post', 'portfolios/5/positions']);
        expect(bodyOf(request)).toEqual({ symbol: 'AAPL', quantity: 2, entryPrice: 150.5, entryDate: '2024-05-06T00:00:00' });
    });

    test('a new quantity is the body of a PUT to the position, not part of its address', async () => {
        const request = await send(() => UpdateStockQuantity(7, 30));
        expect([request.method, request.url]).toEqual(['put', 'positions/7']);
        expect(bodyOf(request)).toEqual({ quantity: 30 });
    });

    test('delete', async () => {
        const request = await send(() => DeleteStock(7));
        expect([request.method, request.url]).toEqual(['delete', 'positions/7']);
    });

    test('refreshing prices changes data, so it is a POST', async () => {
        const request = await send(() => RefreshPrices(5));
        expect([request.method, request.url]).toEqual(['post', 'portfolios/5/prices/refresh']);
    });
});

describe('statistics', () => {
    test('growth', async () => {
        const request = await send(() => GetGrowthStatsData(5));
        expect([request.method, request.url]).toEqual(['get', 'portfolios/5/growth']);
    });

    test('optimisation', async () => {
        const request = await send(() => GetOptimisationData(5));
        expect([request.method, request.url]).toEqual(['get', 'portfolios/5/optimisation']);
    });
});
