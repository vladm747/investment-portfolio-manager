import { buildGrowthSeries, GrowthPoint, periodChange, sliceRange } from './growth';
import StockDto from '../DTO/StockDto';

const stock = (entryDate: string, entryPrice: number, quantity: number) =>
    ({ entryDate, entryPrice, quantity } as unknown as StockDto);

const day = (n: number) => {
    const date = new Date(2024, 0, 1 + n);
    const pad = (value: number) => String(value).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T00:00:00`;
};

describe('buildGrowthSeries', () => {
    test('orders the days and counts a position as invested from its entry date', () => {
        const growth = { [day(2)]: 130, [day(0)]: 100, [day(1)]: 110 };
        const points = buildGrowthSeries(growth, [stock(day(0), 50, 2), stock(day(2), 10, 3)]);

        expect(points.map((point) => point.value)).toEqual([100, 110, 130]);
        expect(points.map((point) => point.invested)).toEqual([100, 100, 130]);
    });

    test('nothing in, nothing out', () => {
        expect(buildGrowthSeries(null, [])).toEqual([]);
        expect(buildGrowthSeries({}, [])).toEqual([]);
    });

    test('ignores values that are not numbers', () => {
        const points = buildGrowthSeries({ [day(0)]: 100, 'not a date': 5 }, []);
        expect(points).toHaveLength(1);
    });
});

describe('sliceRange', () => {
    const points: GrowthPoint[] = Array.from({ length: 400 }, (_, n) => ({ ts: new Date(2024, 0, 1 + n).getTime(), value: n, invested: 0 }));

    test('keeps the last N days counted from the last point', () => {
        const month = sliceRange(points, '1M');
        expect(month[month.length - 1]).toBe(points[points.length - 1]);
        expect(month.length).toBeGreaterThanOrEqual(30);
        expect(month.length).toBeLessThanOrEqual(32);
    });

    test('all returns everything', () => {
        expect(sliceRange(points, 'ALL')).toHaveLength(400);
    });

    test('an empty series stays empty', () => {
        expect(sliceRange([], '3M')).toEqual([]);
    });
});

describe('periodChange', () => {
    test('money added during the period is not counted as a gain', () => {
        const points: GrowthPoint[] = [
            { ts: 1, value: 1000, invested: 1000 },
            { ts: 2, value: 1600, invested: 1500 },
        ];
        const { change, percent } = periodChange(points);
        expect(change).toBe(100);
        // Gain 100 on 1000 at the start plus the 500 added during the period.
        expect(percent).toBeCloseTo(6.6667, 3);
    });

    test('fewer than two points means no change', () => {
        expect(periodChange([])).toEqual({ change: 0, percent: 0 });
    });
});
