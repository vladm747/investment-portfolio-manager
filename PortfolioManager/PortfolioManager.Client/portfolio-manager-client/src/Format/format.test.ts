import {
    directionOf, EMPTY_VALUE, formatDate, formatDateLong, formatMoney, formatNumber, formatPercent,
    formatMonthYear, formatUpdatedAt, positionsCount,
} from './format';

/** Intl separates thousands with a non-breaking space; the tests compare with an ordinary one. */
const plain = (text: string) => text.replace(/[  ]/g, ' ');

describe('formatMoney', () => {
    test('uses a space for thousands, a comma for decimals and the dollar sign', () => {
        expect(plain(formatMoney(16846.2))).toBe('16 846,20 $');
    });

    test('shows a plus for gains and a real minus sign for losses when signed', () => {
        expect(plain(formatMoney(4345.7, { signed: true }))).toBe('+4 345,70 $');
        expect(plain(formatMoney(-237.7, { signed: true }))).toBe('−237,70 $');
    });

    test('shows no sign for zero', () => {
        expect(plain(formatMoney(0, { signed: true }))).toBe('0,00 $');
    });

    test('knows other currencies and fewer digits', () => {
        expect(plain(formatMoney(1234.5, { currency: 'EUR' }))).toBe('1 234,50 €');
        expect(plain(formatMoney(16846.2, { digits: 0 }))).toBe('16 846 $');
    });

    test('a missing amount is a dash, not NaN', () => {
        expect(formatMoney(null)).toBe(EMPTY_VALUE);
        expect(formatMoney(undefined)).toBe(EMPTY_VALUE);
        expect(formatMoney(Number.NaN)).toBe(EMPTY_VALUE);
    });
});

describe('formatPercent', () => {
    test('takes percent units and writes a comma', () => {
        expect(formatPercent(34.76)).toBe('34,76%');
    });

    test('signs gains and losses when asked', () => {
        expect(formatPercent(34.76, { signed: true })).toBe('+34,76%');
        expect(formatPercent(-2.41, { signed: true })).toBe('−2,41%');
    });

    test('rounding to zero leaves no sign', () => {
        expect(formatPercent(-0.001, { signed: true })).toBe('0,00%');
    });

    test('digits are configurable', () => {
        expect(formatPercent(12.5, { digits: 1 })).toBe('12,5%');
    });
});

test('formatNumber groups thousands and keeps the requested digits', () => {
    expect(plain(formatNumber(1234567.891, 2))).toBe('1 234 567,89');
    expect(formatNumber(1.4567, 2)).toBe('1,46');
    expect(formatNumber(undefined)).toBe(EMPTY_VALUE);
});

describe('dates', () => {
    test('short numeric date, long date and a dash for nothing', () => {
        expect(formatDate(new Date(2024, 2, 12))).toBe('12.03.2024');
        expect(formatDateLong(new Date(2024, 2, 12))).toBe('12 березня 2024 р.');
        expect(formatDate('2024-03-12T00:00:00')).toBe('12.03.2024');
        expect(formatDate(null)).toBe(EMPTY_VALUE);
        expect(formatDate('not a date')).toBe(EMPTY_VALUE);
    });

    test('the update time says today, yesterday or the day', () => {
        const now = new Date(2026, 10, 5, 18, 0);
        expect(formatUpdatedAt(new Date(2026, 10, 5, 14, 32), now)).toBe('сьогодні о 14:32');
        expect(formatUpdatedAt(new Date(2026, 10, 4, 9, 5), now)).toBe('учора о 09:05');
        expect(formatUpdatedAt(new Date(2026, 9, 20, 9, 5), now)).toBe('20 жовтня о 09:05');
    });
});

test('the month and year for an axis have no trailing «р.»', () => {
    expect(formatMonthYear(new Date(2025, 10, 5))).toBe('лист. 2025');
});

test('counts positions with the right Ukrainian plural form', () => {
    expect(positionsCount(1)).toBe('1 позиція');
    expect(positionsCount(3)).toBe('3 позиції');
    expect(positionsCount(5)).toBe('5 позицій');
    expect(positionsCount(21)).toBe('21 позиція');
    expect(positionsCount(0)).toBe('0 позицій');
});

test('directionOf calls an amount that rounds to zero flat', () => {
    expect(directionOf(0.4)).toBe('up');
    expect(directionOf(-0.4)).toBe('down');
    expect(directionOf(0.001)).toBe('flat');
    expect(directionOf(null)).toBe('flat');
});
