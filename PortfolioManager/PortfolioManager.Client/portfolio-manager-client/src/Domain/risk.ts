import RiskForecastDto, { RiskFiguresDto, RiskMetricDto, RiskModelDto } from '../DTO/RiskForecastDto';
import { EMPTY_VALUE, formatDate, formatPercent, pluralize } from '../Format/format';
import { niceScale } from './niceScale';

/**
 * The risk forecast in words: what each metric means, how to write it, and what the warnings say.
 * Every rule about the three metrics lives here, keyed like the API's fields.
 */

export type RiskMetricKey = 'volatility' | 'valueAtRisk95' | 'maxDrawdown';

export interface RiskMetricInfo {
    key: RiskMetricKey;
    label: string;
    /** For a narrow column or a toggle button. */
    shortLabel: string;
    /** Shown behind the info button. */
    description: string;
    /** Digits after the comma: VaR is a couple of percent, so it needs two. */
    digits: number;
    /** The loss in money is shown next to it. */
    inMoney: boolean;
}

export const RISK_METRICS: RiskMetricInfo[] = [
    {
        key: 'volatility',
        label: 'Волатильність (річна)',
        shortLabel: 'Волатильність',
        description:
            'Наскільки сильно коливатиметься дохідність протягом наступних трьох місяців, у перерахунку на рік. Що менша, то стабільніший портфель.',
        digits: 1,
        inMoney: false,
    },
    {
        key: 'valueAtRisk95',
        label: 'VaR 95% за день',
        shortLabel: 'VaR 95%',
        description:
            'Денний збиток, який буде перевищено в середньому лише в одному торговому дні з двадцяти протягом наступних трьох місяців.',
        digits: 2,
        inMoney: true,
    },
    {
        key: 'maxDrawdown',
        label: 'Максимальна просадка',
        shortLabel: 'Просадка',
        description: 'Найбільше падіння вартості від піку до дна протягом наступних трьох місяців.',
        digits: 1,
        inMoney: true,
    },
];

export const riskMetricInfo = (key: RiskMetricKey): RiskMetricInfo =>
    RISK_METRICS.find((metric) => metric.key === key) as RiskMetricInfo;

/** A fraction from the API as a percentage: 0.1907 is «19,1%» for volatility, 0.02 is «2,00%» for VaR. */
export function formatRisk(key: RiskMetricKey, fraction: number | null | undefined): string {
    return fraction === null || fraction === undefined ? EMPTY_VALUE : formatPercent(fraction * 100, { digits: riskMetricInfo(key).digits });
}

/** «13,7–25,2%», or a dash when either end is unknown. */
export function formatRiskInterval(key: RiskMetricKey, metric: Pick<RiskMetricDto, 'low' | 'high'>): string {
    if (metric.low === null || metric.high === null) {
        return EMPTY_VALUE;
    }
    return `${formatRisk(key, metric.low).replace('%', '')}–${formatRisk(key, metric.high)}`;
}

/** «3 місяці (63 торгові дні)» */
export const horizonText = (model: Pick<RiskModelDto, 'horizonDays'>) =>
    `3 місяці (${pluralize(model.horizonDays, { one: 'торговий день', few: 'торгові дні', many: 'торгових днів' })})`;

/** «у 8 випадках із 10» for an 80% interval. */
export const intervalMeaning = (model: Pick<RiskModelDto, 'intervalLevel'>) =>
    `у ${Math.round(model.intervalLevel * 10)} випадках із 10 фактичне значення потрапляє в цей діапазон`;

/** The model's features in Ukrainian, for the warning that names those outside the range it was trained on. */
export const FEATURE_LABELS: Record<string, string> = {
    ret_21: 'дохідність за місяць',
    ret_63: 'дохідність за 3 місяці',
    ret_126: 'дохідність за 6 місяців',
    ret_252: 'дохідність за рік',
    mom_12_1: 'імпульс за рік без останнього місяця',
    vol_21: 'волатильність за місяць',
    vol_63: 'волатильність за 3 місяці',
    vol_252: 'волатильність за рік',
    ewma_vol: 'експоненційна волатильність (EWMA)',
    dvol_63: 'волатильність падінь за 3 місяці',
    vol_ratio: 'відношення місячної волатильності до річної',
    dd_252: 'просадка від річного максимуму',
    skew_63: 'асиметрія дохідностей',
    ma50_dist: 'відхилення від 50-денної середньої',
    ma200_dist: 'відхилення від 200-денної середньої',
    rsi_14: 'індекс відносної сили (RSI)',
    beta_252: 'бета щодо ринку',
    rel_ret_252: 'дохідність відносно ринку за рік',
    rel_vol_63: 'волатильність відносно ринку',
    mkt_ret_63: 'дохідність ринку за 3 місяці',
    mkt_vol_21: 'волатильність ринку за місяць',
    mkt_vol_63: 'волатильність ринку за 3 місяці',
    mkt_dd_252: 'просадка ринку від річного максимуму',
    vix: 'індекс волатильності VIX',
};

export const featureLabel = (name: string) => FEATURE_LABELS[name] ?? name;

/** Why a forecast is marked unusual, in one sentence. */
export function outOfDomainReason(figures: Pick<RiskFiguresDto, 'outOfDomainFeatures' | 'staleQuotes'>): string {
    const parts: string[] = [];
    if (figures.outOfDomainFeatures.length > 0) {
        parts.push(`поза межами навчальних даних: ${figures.outOfDomainFeatures.map(featureLabel).join(', ')}`);
    }
    if (figures.staleQuotes) {
        parts.push('ціна надто часто не змінюється від дня до дня');
    }
    return parts.length > 0 ? `${parts.join('; ')}.` : 'деякі показники нетипові для навчальних даних.';
}

export interface RiskNotice {
    severity: 'warning' | 'info';
    title: string;
    text: string;
}

/** What the user should know before trusting the figures: an old model, an unusual portfolio or stock, a missing forecast. */
export function riskNotices(forecast: Pick<RiskForecastDto, 'model' | 'portfolio' | 'assets'>): RiskNotice[] {
    const notices: RiskNotice[] = [];
    const { model, portfolio, assets } = forecast;

    if (model.stale) {
        notices.push({
            severity: 'warning',
            title: 'Модель потребує оновлення',
            text: `Модель навчено на цінах до ${formatDate(model.trainedThrough)}: з того часу минуло ${pluralize(model.ageDays, { one: 'день', few: 'дні', many: 'днів' })}, а оновлювати її слід щонайменше раз на ${model.staleAfterDays} днів. Прогноз може бути менш точним.`,
        });
    }

    if (portfolio.outOfDomain) {
        notices.push({
            severity: 'warning',
            title: 'Портфель нетиповий для моделі',
            text: `Прогноз розраховано, але він менш надійний: ${outOfDomainReason(portfolio)}`,
        });
    }

    const unusual = assets.filter((asset) => asset.forecast?.outOfDomain).map((asset) => asset.symbol);
    if (unusual.length > 0) {
        notices.push({
            severity: 'info',
            title: unusual.length === 1 ? 'Нетипова акція' : 'Нетипові акції',
            text: `${unusual.join(', ')}: поведінка ціни виходить за межі даних, на яких навчено модель, тож окремий прогноз менш надійний.`,
        });
    }

    const missing = assets.filter((asset) => !asset.forecast).map((asset) => asset.symbol);
    if (missing.length > 0) {
        notices.push({
            severity: 'info',
            title: 'Не для всіх акцій є окремий прогноз',
            text: `${missing.join(', ')}: у цінах за останній рік є пропуски або тривалі періоди без змін, тож окремий прогноз не розраховано. У прогнозі портфеля ці акції враховано.`,
        });
    }

    return notices;
}

/** About this many ticks on a risk axis: enough that the axis ends close past the largest value. */
export const RISK_AXIS_TICKS = 6;

/** The right end of a shared axis for these metrics: a round number past every bound and past value. */
export function riskScaleMax(metrics: (RiskMetricDto | null | undefined)[]): number {
    const values = metrics.flatMap((metric) =>
        metric ? [metric.value, metric.low, metric.high, metric.previous].filter((value): value is number => value !== null && Number.isFinite(value)) : []
    );
    const max = Math.max(0, ...values);
    return max > 0 ? niceScale(0, max, RISK_AXIS_TICKS).domain[1] : 1;
}

/** Where a value sits along an axis from 0 to `max`, in percent of its width, kept inside the axis. */
export const positionOn = (value: number, max: number) => Math.min(100, Math.max(0, (value / max) * 100));

/** For a risk metric a lower value is better. Values that look the same on screen count as the same. */
export function compareRisk(key: RiskMetricKey, current: number | null, optimised: number | null): 'better' | 'worse' | 'same' | null {
    if (current === null || optimised === null) {
        return null;
    }
    const digits = riskMetricInfo(key).digits + 2;
    const a = Number(current.toFixed(digits));
    const b = Number(optimised.toFixed(digits));
    return a === b ? 'same' : b < a ? 'better' : 'worse';
}
