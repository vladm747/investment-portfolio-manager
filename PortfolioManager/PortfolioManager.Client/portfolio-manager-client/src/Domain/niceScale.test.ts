import { niceScale } from './niceScale';

test('ticks are round numbers that enclose the data', () => {
    const { ticks, domain } = niceScale(13644, 17082);
    expect(ticks).toEqual([13000, 14000, 15000, 16000, 17000, 18000]);
    expect(domain).toEqual([13000, 18000]);

    // A wider range gets a coarser step, so there are never too many ticks.
    expect(niceScale(12500, 17082).ticks).toEqual([12000, 14000, 16000, 18000]);
});

test('a range starting near zero starts at zero', () => {
    const { ticks } = niceScale(2606, 18185);
    expect(ticks[0]).toBe(0);
    expect(ticks[ticks.length - 1]).toBeGreaterThanOrEqual(18185);
});

test('a flat line gets room above and below', () => {
    const { domain } = niceScale(1000, 1000);
    expect(domain[0]).toBeLessThan(1000);
    expect(domain[1]).toBeGreaterThan(1000);
});

test('values that are not finite do not crash', () => {
    expect(niceScale(Number.NaN, 5).ticks).toEqual([0, 1]);
});
