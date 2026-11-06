const keyOf = (portfolioId: number) => `portfolioManager.pricesUpdatedAt.${portfolioId}`;

/**
 * When the prices of a portfolio were last refreshed from this browser. The server does not report it,
 * so it is remembered here; storage that is blocked just means the time is not shown.
 */
export function getPricesUpdatedAt(portfolioId: number): Date | null {
    try {
        const text = window.localStorage.getItem(keyOf(portfolioId));
        const date = text ? new Date(text) : null;
        return date && !Number.isNaN(date.getTime()) ? date : null;
    } catch {
        return null;
    }
}

export function setPricesUpdatedAt(portfolioId: number, when: Date = new Date()) {
    try {
        window.localStorage.setItem(keyOf(portfolioId), when.toISOString());
    } catch {
        // Not shown next time; nothing else depends on it.
    }
}
