import { parseDecimal, parseWholeNumber, validatePosition, validateQuantity } from './validation';

test('decimals are read with a comma or a dot', () => {
    expect(parseDecimal('172,40')).toBe(172.4);
    expect(parseDecimal(' 172.40 ')).toBe(172.4);
    expect(parseDecimal('1 250,5')).toBe(1250.5);
    expect(parseDecimal('abc')).toBeNaN();
    expect(parseDecimal('-5')).toBeNaN();
    expect(parseDecimal('')).toBeNaN();
});

test('whole numbers accept digits only', () => {
    expect(parseWholeNumber('15')).toBe(15);
    expect(parseWholeNumber('1.5')).toBeNaN();
    expect(parseWholeNumber('')).toBeNaN();
});

test('quantity must be a positive whole number', () => {
    expect(validateQuantity('3')).toBeNull();
    expect(validateQuantity('0')).not.toBeNull();
    expect(validateQuantity('2.5')).not.toBeNull();
    expect(validateQuantity('')).not.toBeNull();
});

describe('validatePosition', () => {
    const today = new Date(2026, 10, 5);
    const valid = { symbol: 'aapl', quantity: '15', entryPrice: '172,40', entryDate: '2024-03-12' };

    test('a good position has no errors', () => {
        expect(validatePosition(valid, today)).toEqual({});
    });

    test('every bad field gets its own message', () => {
        const errors = validatePosition({ symbol: '1X', quantity: '0', entryPrice: '0', entryDate: '' }, today);
        expect(Object.keys(errors).sort()).toEqual(['entryDate', 'entryPrice', 'quantity', 'symbol']);
    });

    test('tickers may have a dot or a dash', () => {
        expect(validatePosition({ ...valid, symbol: 'BRK.B' }, today).symbol).toBeUndefined();
        expect(validatePosition({ ...valid, symbol: 'TOOLONGSYMBOL' }, today).symbol).toBeDefined();
    });

    test('the entry date cannot be in the future', () => {
        expect(validatePosition({ ...valid, entryDate: '2026-11-06' }, today).entryDate).toBeDefined();
        expect(validatePosition({ ...valid, entryDate: '2026-11-05' }, today).entryDate).toBeUndefined();
    });
});
