/**
 * Round axis ticks for a value range, e.g. 12 000, 14 000 … 20 000. The first and last tick enclose the
 * range, so the axis can use them as its domain.
 */
export function niceScale(min: number, max: number, approxTickCount = 5): { ticks: number[]; domain: [number, number] } {
    if (!Number.isFinite(min) || !Number.isFinite(max)) {
        return { ticks: [0, 1], domain: [0, 1] };
    }
    if (min === max) {
        // A flat line still needs some room around it.
        const pad = Math.abs(min) * 0.05 || 1;
        min -= pad;
        max += pad;
    }

    const rough = (max - min) / Math.max(1, approxTickCount - 1);
    const magnitude = Math.pow(10, Math.floor(Math.log10(rough)));
    const step = [1, 2, 2.5, 5, 10].map((factor) => factor * magnitude).find((candidate) => candidate >= rough) ?? 10 * magnitude;

    const first = Math.floor(min / step) * step;
    const last = Math.ceil(max / step) * step;
    const ticks: number[] = [];
    for (let value = first; value <= last + step / 2; value += step) {
        ticks.push(Math.round(value * 1e6) / 1e6);
    }
    return { ticks, domain: [ticks[0], ticks[ticks.length - 1]] };
}
