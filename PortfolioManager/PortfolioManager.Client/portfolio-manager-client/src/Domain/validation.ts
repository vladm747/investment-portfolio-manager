/** Reads «172,40» as well as «172.40»: people type the comma. Returns NaN for anything that is not a plain number. */
export function parseDecimal(text: string): number {
    const normalised = text.trim().replace(/\s/g, '').replace(',', '.');
    return /^\d+(\.\d+)?$/.test(normalised) ? Number(normalised) : Number.NaN;
}

export function parseWholeNumber(text: string): number {
    const trimmed = text.trim();
    return /^\d+$/.test(trimmed) ? Number(trimmed) : Number.NaN;
}

/** The API stores Stock.Quantity as an int. */
export const MAX_QUANTITY = 2_000_000_000;

export function validateQuantity(text: string): string | null {
    const value = parseWholeNumber(text);
    if (Number.isNaN(value) || value < 1 || value > MAX_QUANTITY) {
        return 'Кількість має бути цілим числом, більшим за нуль.';
    }
    return null;
}

export interface PositionForm {
    symbol: string;
    quantity: string;
    entryPrice: string;
    /** yyyy-mm-dd, as an <input type="date"> gives it. */
    entryDate: string;
}

export type PositionErrors = Partial<Record<keyof PositionForm, string>>;

/** Stock.Symbol is limited to 10 characters in the database. */
const SYMBOL_PATTERN = /^[A-Za-z][A-Za-z0-9.\-]{0,9}$/;

export function validatePosition(form: PositionForm, today: Date = new Date()): PositionErrors {
    const errors: PositionErrors = {};

    if (!SYMBOL_PATTERN.test(form.symbol.trim())) {
        errors.symbol = 'Тікер: до 10 символів, латинські літери та цифри, наприклад AAPL.';
    }

    const quantityError = validateQuantity(form.quantity);
    if (quantityError) {
        errors.quantity = quantityError;
    }

    const price = parseDecimal(form.entryPrice);
    if (Number.isNaN(price) || price <= 0) {
        errors.entryPrice = 'Вкажіть ціну входу більшу за нуль.';
    }

    const todayText = [today.getFullYear(), String(today.getMonth() + 1).padStart(2, '0'), String(today.getDate()).padStart(2, '0')].join('-');
    if (!/^\d{4}-\d{2}-\d{2}$/.test(form.entryDate) || Number.isNaN(new Date(form.entryDate).getTime())) {
        errors.entryDate = 'Вкажіть дату входу.';
    } else if (form.entryDate > todayText) {
        errors.entryDate = 'Дата входу не може бути в майбутньому.';
    } else if (form.entryDate < '1970-01-01') {
        errors.entryDate = 'Дата входу занадто рання.';
    }

    return errors;
}
