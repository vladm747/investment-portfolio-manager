import { formatNumber, formatPercent } from '../Format/format';

/** `percent`: the API sends a fraction (0.237 is 23,7%). `ratio`: a plain number such as the Sharpe ratio. */
export type MetricKind = 'percent' | 'ratio';

export interface MetricInfo {
    kind: MetricKind;
    /** Which way is better. A drawdown is stored as a negative number, so it is compared by size. */
    better: 'higher' | 'lower';
    compareBy?: 'magnitude';
    /** Shown in the info tooltip next to the metric's name. */
    description: string;
}

/**
 * Every metric the comparison can show, keyed by the exact name the API sends. The rules live only here;
 * a metric the map does not know is shown as a plain number where higher is better.
 */
export const METRICS: Record<string, MetricInfo> = {
    'Загальний дохід': {
        kind: 'percent',
        better: 'higher',
        description: 'На скільки відсотків зросла вартість портфеля за весь розрахунковий період.',
    },
    'Середньорічний темп приросту (CAGR﹪)': {
        kind: 'percent',
        better: 'higher',
        description: 'Середня річна дохідність, якщо прибуток щороку реінвестується.',
    },
    'Коефіцієнт Шарпа': {
        kind: 'ratio',
        better: 'higher',
        description: 'Дохідність понад безризикову ставку на одиницю ризику. Що більше, то краще.',
    },
    'Коефіцієнт Сортіно': {
        kind: 'ratio',
        better: 'higher',
        description: 'Як коефіцієнт Шарпа, але враховує лише коливання вниз: «поганий» ризик.',
    },
    'Коефіцієнт Кальмара': {
        kind: 'ratio',
        better: 'higher',
        description: 'Річна дохідність, поділена на максимальну просадку.',
    },
    'Максимальна просадка': {
        kind: 'percent',
        better: 'lower',
        compareBy: 'magnitude',
        description: 'Найбільше падіння вартості від піку до дна за період. Що менша просадка, то краще.',
    },
    'Показник волатильності (річний)': {
        kind: 'percent',
        better: 'lower',
        description: 'Розмах коливань дохідності за рік. Менша волатильність означає стабільніший портфель.',
    },
    'Прогнозований місячний дохід': {
        kind: 'percent',
        better: 'higher',
        description: 'Очікувана дохідність за місяць, якщо минулі результати повторяться.',
    },
    'Прогнозований річний дохід': {
        kind: 'percent',
        better: 'higher',
        description: 'Очікувана дохідність за рік, якщо минулі результати повторяться.',
    },
};

const FALLBACK: MetricInfo = { kind: 'ratio', better: 'higher', description: '' };

export const metricInfo = (name: string): MetricInfo => METRICS[name] ?? FALLBACK;

export type Verdict = 'better' | 'worse' | 'same';

/** Is the optimised value better than the current one, by this metric's own rule? */
export function compareMetric(name: string, current: number, optimised: number): Verdict {
    const info = metricInfo(name);
    const size = (value: number) => (info.compareBy === 'magnitude' ? Math.abs(value) : value);
    // Values that look the same on screen count as the same.
    const round = (value: number) => Number(size(value).toFixed(4));
    const a = round(current);
    const b = round(optimised);

    if (a === b) return 'same';
    const optimisedIsHigher = b > a;
    return optimisedIsHigher === (info.better === 'higher') ? 'better' : 'worse';
}

export function formatMetricValue(name: string, value: number): string {
    return metricInfo(name).kind === 'percent' ? formatPercent(value * 100) : formatNumber(value, 2);
}

/** The change from current to optimised: «+3,66 п.п.» for percentages, «+0,28» for ratios. */
export function formatMetricChange(name: string, current: number, optimised: number): string {
    const difference = optimised - current;
    if (metricInfo(name).kind === 'percent') {
        return `${formatPercent(difference * 100, { signed: true })} п.п.`.replace('%', '');
    }
    const text = formatNumber(Math.abs(difference), 2);
    return Number(difference.toFixed(2)) === 0 ? text : `${difference > 0 ? '+' : '−'}${text}`;
}

export interface WeightRow {
    symbol: string;
    /** Shares in percent, 0 to 100. */
    current: number;
    optimised: number;
}

/** Lines the API's two weight lists up by ticker: the base weights follow the order of the optimised ones. */
export function toWeightRows(baseWeights: number[], optimisedWeights: Record<string, number>): WeightRow[] {
    return Object.entries(optimisedWeights).map(([symbol, optimised], index) => ({
        symbol,
        current: (baseWeights[index] ?? 0) * 100,
        optimised: optimised * 100,
    }));
}

/**
 * Which change of the raw number is good news. A drawdown is negative, so a smaller one is a larger number
 * (−28% is above −30%); volatility is positive, so a smaller one is a smaller number.
 */
export function goodDirection(name: string): 'up' | 'down' {
    const info = metricInfo(name);
    if (info.better === 'higher') return 'up';
    return info.compareBy === 'magnitude' ? 'up' : 'down';
}
