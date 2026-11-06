/**
 * Numbers and dates the way a Ukrainian reader expects them: «16 846,20 $», «+34,76%», «12.03.2024».
 * Every figure on screen goes through these helpers, so the format is the same everywhere.
 */
const LOCALE = 'uk-UA';
const MINUS = '−';

/** Intl writes a hyphen; a real minus sign is wider and lines up with the plus sign. */
const withMinusSign = (text: string) => text.replace('-', MINUS);

const isMissing = (value: number | null | undefined): value is null | undefined =>
    value === null || value === undefined || Number.isNaN(value);

export const EMPTY_VALUE = '—';

export interface MoneyOptions {
    /** ISO currency code; the dollar is the default because portfolio totals carry no currency. */
    currency?: string;
    /** Show «+» for a positive amount (and «−» for a negative one). */
    signed?: boolean;
    /** Digits after the comma, 2 by default. */
    digits?: number;
}

export function formatMoney(value: number | null | undefined, options: MoneyOptions = {}): string {
    if (isMissing(value)) {
        return EMPTY_VALUE;
    }
    const { currency = 'USD', signed = false, digits = 2 } = options;
    const text = new Intl.NumberFormat(LOCALE, {
        style: 'currency',
        currency,
        currencyDisplay: 'narrowSymbol',
        minimumFractionDigits: digits,
        maximumFractionDigits: digits,
        signDisplay: signed ? 'exceptZero' : 'auto',
    }).format(value);
    return withMinusSign(text);
}

export interface PercentOptions {
    signed?: boolean;
    digits?: number;
}

/** `value` is already in percent: 5.12 gives «5,12%». */
export function formatPercent(value: number | null | undefined, options: PercentOptions = {}): string {
    if (isMissing(value)) {
        return EMPTY_VALUE;
    }
    const { signed = false, digits = 2 } = options;
    const text = new Intl.NumberFormat(LOCALE, {
        style: 'percent',
        minimumFractionDigits: digits,
        maximumFractionDigits: digits,
        signDisplay: signed ? 'exceptZero' : 'auto',
    }).format(value / 100);
    return withMinusSign(text);
}

export function formatNumber(value: number | null | undefined, digits = 0): string {
    if (isMissing(value)) {
        return EMPTY_VALUE;
    }
    return withMinusSign(
        new Intl.NumberFormat(LOCALE, { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(value)
    );
}

type DateInput = Date | string | number | null | undefined;

function toDate(value: DateInput): Date | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }
    const date = value instanceof Date ? value : new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
}

/** «12.03.2024» */
export function formatDate(value: DateInput): string {
    const date = toDate(value);
    return date ? new Intl.DateTimeFormat(LOCALE).format(date) : EMPTY_VALUE;
}

/** «12 березня 2024 р.» */
export function formatDateLong(value: DateInput): string {
    const date = toDate(value);
    return date
        ? new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'long', year: 'numeric' }).format(date)
        : EMPTY_VALUE;
}

/** «12 бер.»: the short form for chart axes. */
export function formatDateShort(value: DateInput): string {
    const date = toDate(value);
    return date ? new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'short' }).format(date) : EMPTY_VALUE;
}

/** «бер. 2024»: for chart axes that span a year or more. */
export function formatMonthYear(value: DateInput): string {
    const date = toDate(value);
    // Intl adds «р.» (рік) after the year, which only clutters an axis.
    return date
        ? new Intl.DateTimeFormat(LOCALE, { month: 'short', year: 'numeric' }).format(date).replace(/\s?р\.$/, '')
        : EMPTY_VALUE;
}

/** Where the time of day matters: «сьогодні о 14:32», «учора о 09:05» or «5 листопада о 14:32». */
export function formatUpdatedAt(value: DateInput, now: Date = new Date()): string {
    const date = toDate(value);
    if (!date) {
        return EMPTY_VALUE;
    }
    const time = new Intl.DateTimeFormat(LOCALE, { hour: '2-digit', minute: '2-digit' }).format(date);
    const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
    const daysAgo = Math.round((startOfDay(now) - startOfDay(date)) / 86_400_000);

    if (daysAgo === 0) return `сьогодні о ${time}`;
    if (daysAgo === 1) return `учора о ${time}`;
    return `${new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'long' }).format(date)} о ${time}`;
}

/** «1 позиція», «2 позиції», «5 позицій» */
export function pluralize(count: number, forms: { one: string; few: string; many: string }): string {
    const category = new Intl.PluralRules(LOCALE).select(count);
    const word = category === 'one' ? forms.one : category === 'few' ? forms.few : forms.many;
    return `${formatNumber(count)} ${word}`;
}

export const positionsCount = (count: number) =>
    pluralize(count, { one: 'позиція', few: 'позиції', many: 'позицій' });

/** The sign of a change, with amounts that round to zero counted as no change. */
export type Direction = 'up' | 'down' | 'flat';

export function directionOf(value: number | null | undefined, digits = 2): Direction {
    if (isMissing(value)) {
        return 'flat';
    }
    const rounded = Number(value.toFixed(digits));
    return rounded > 0 ? 'up' : rounded < 0 ? 'down' : 'flat';
}
