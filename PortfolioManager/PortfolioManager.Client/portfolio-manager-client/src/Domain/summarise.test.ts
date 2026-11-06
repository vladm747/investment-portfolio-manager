import { summarise } from './summarise';
import StockDto from '../DTO/StockDto';

const position = (entryPrice: number, quantity: number, totalCurrentPrice: number) =>
    ({ entryPrice, quantity, totalCurrentPrice } as StockDto);

test('adds up what was paid and what the positions are worth now', () => {
    const result = summarise([position(100, 2, 260), position(50, 10, 450)]);

    expect(result.invested).toBe(700);
    expect(result.current).toBe(710);
    expect(result.gain).toBe(10);
    expect(result.gainPercentage).toBeCloseTo(1.4286, 4);
});

test('a loss is negative', () => {
    const result = summarise([position(100, 1, 80)]);

    expect(result.gain).toBe(-20);
    expect(result.gainPercentage).toBe(-20);
});

test('an empty portfolio has zero totals and no division by zero', () => {
    expect(summarise([])).toEqual({ invested: 0, current: 0, gain: 0, gainPercentage: 0 });
});
