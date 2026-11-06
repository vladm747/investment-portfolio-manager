import StockDto from "../DTO/StockDto";

export interface GrowthPoint {
    /** Midnight of the day, in milliseconds. */
    ts: number;
    /** What the portfolio was worth. */
    value: number;
    /** What had been put in by then: entry price × quantity of every position bought on or before the day. */
    invested: number;
}

export type GrowthRange = '1M' | '3M' | '6M' | '1Y' | 'ALL';

export const GROWTH_RANGES: { key: GrowthRange; label: string; days: number | null }[] = [
    { key: '1M', label: '1М', days: 30 },
    { key: '3M', label: '3М', days: 91 },
    { key: '6M', label: '6М', days: 182 },
    { key: '1Y', label: '1Р', days: 365 },
    { key: 'ALL', label: 'Усе', days: null },
];

const DAY = 86_400_000;

/** Turns the API's { "2024-03-12T00:00:00": value } into ordered points with the invested amount beside each. */
export function buildGrowthSeries(growthData: Record<string, number> | null | undefined, stocks: StockDto[]): GrowthPoint[] {
    if (!growthData) {
        return [];
    }
    const purchases = stocks
        .map((stock) => ({ ts: new Date(stock.entryDate).getTime(), amount: stock.entryPrice * stock.quantity }))
        .filter((purchase) => Number.isFinite(purchase.ts));

    return Object.entries(growthData)
        .map(([date, value]) => ({ ts: new Date(date).getTime(), value: Number(value) }))
        .filter((point) => Number.isFinite(point.ts) && Number.isFinite(point.value))
        .sort((a, b) => a.ts - b.ts)
        .map((point) => ({
            ...point,
            invested: purchases.reduce((sum, purchase) => (purchase.ts <= point.ts ? sum + purchase.amount : sum), 0),
        }));
}

/** The last N days of the series, counted back from its last point. */
export function sliceRange(points: GrowthPoint[], range: GrowthRange): GrowthPoint[] {
    const days = GROWTH_RANGES.find((item) => item.key === range)?.days ?? null;
    if (days === null || points.length === 0) {
        return points;
    }
    const from = points[points.length - 1].ts - days * DAY;
    return points.filter((point) => point.ts >= from);
}

/**
 * How much the portfolio gained over the shown period. Money added during the period (a position bought
 * inside it) is not a gain, so it is taken out of the change.
 */
export function periodChange(points: GrowthPoint[]): { change: number; percent: number } {
    if (points.length < 2) {
        return { change: 0, percent: 0 };
    }
    const first = points[0];
    const last = points[points.length - 1];
    const added = last.invested - first.invested;
    const change = last.value - first.value - added;
    const base = first.value + added;
    return { change, percent: base > 0 ? (change / base) * 100 : 0 };
}
