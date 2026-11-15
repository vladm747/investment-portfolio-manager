import { compareRisk, featureLabel, FEATURE_LABELS, formatRisk, formatRiskInterval, horizonText, intervalMeaning, outOfDomainReason, positionOn, riskNotices, riskScaleMax } from './risk';
import { makeRiskForecast, portfolioFigures, stockFigures } from '../testing/riskForecast';

const plain = (text: string) => text.replace(/[\u00a0\u202f]/g, ' ');

describe('formatting', () => {
    test('volatility and drawdown with one digit, VaR with two, because it is a couple of percent', () => {
        expect(formatRisk('volatility', 0.19072639451457588)).toBe('19,1%');
        expect(formatRisk('valueAtRisk95', 0.020012388234976208)).toBe('2,00%');
        expect(formatRisk('maxDrawdown', 0.1325217168273489)).toBe('13,3%');
        expect(formatRisk('volatility', null)).toBe('—');
    });

    test('an interval is written once with the percent sign at the end', () => {
        expect(formatRiskInterval('volatility', portfolioFigures.volatility)).toBe('13,7–25,2%');
        expect(formatRiskInterval('valueAtRisk95', portfolioFigures.valueAtRisk95)).toBe('1,21–2,55%');
        expect(formatRiskInterval('volatility', { low: null, high: 0.2 })).toBe('—');
    });

    test('the horizon and the interval in words', () => {
        expect(plain(horizonText({ horizonDays: 63 }))).toBe('3 місяці (63 торгові дні)');
        expect(intervalMeaning({ intervalLevel: 0.8 })).toBe('у 8 випадках із 10 фактичне значення потрапляє в цей діапазон');
    });
});

describe('features', () => {
    test('every feature of the model has a Ukrainian name', () => {
        const modelFeatures = [
            'ret_21', 'ret_63', 'ret_126', 'ret_252', 'mom_12_1', 'vol_21', 'vol_63', 'vol_252', 'ewma_vol', 'dvol_63',
            'vol_ratio', 'dd_252', 'skew_63', 'ma50_dist', 'ma200_dist', 'rsi_14', 'beta_252', 'rel_ret_252', 'rel_vol_63',
            'mkt_ret_63', 'mkt_vol_21', 'mkt_vol_63', 'mkt_dd_252', 'vix',
        ]; // PortfolioManager.MathModule/models/risk-v1/meta.json "features"
        expect(Object.keys(FEATURE_LABELS).sort()).toEqual([...modelFeatures].sort());
        expect(featureLabel('vol_252')).toBe('волатильність за рік');
        expect(featureLabel('new_feature')).toBe('new_feature');
    });

    test('an unusual forecast says why', () => {
        expect(outOfDomainReason({ outOfDomainFeatures: ['vol_252', 'rel_vol_63'], staleQuotes: false })).toBe(
            'поза межами навчальних даних: волатильність за рік, волатильність відносно ринку.'
        );
        expect(outOfDomainReason({ outOfDomainFeatures: [], staleQuotes: true })).toBe('ціна надто часто не змінюється від дня до дня.');
    });
});

describe('riskNotices', () => {
    test('nothing to say about a fresh model and an ordinary portfolio', () => {
        expect(riskNotices(makeRiskForecast())).toEqual([]);
    });

    test('an old model is a warning that says how old', () => {
        const forecast = makeRiskForecast({
            model: { version: 'risk-v1', trainedThrough: '2026-11-13', horizonDays: 63, intervalLevel: 0.8, ageDays: 95, staleAfterDays: 60, stale: true },
        });

        const [notice] = riskNotices(forecast);

        expect(notice.severity).toBe('warning');
        expect(plain(notice.text)).toContain('13.11.2026');
        expect(plain(notice.text)).toContain('95 днів');
        expect(plain(notice.text)).toContain('раз на 60 днів');
    });

    test('an unusual portfolio, unusual stocks and stocks without a forecast each get their notice', () => {
        const forecast = makeRiskForecast({
            portfolio: { ...portfolioFigures, outOfDomain: true, outOfDomainFeatures: ['vol_21'] },
            assets: [
                { symbol: 'GME', weight: 0.5, forecast: { ...stockFigures, outOfDomain: true, outOfDomainFeatures: [], staleQuotes: true } },
                { symbol: 'NEW', weight: 0.3, forecast: null },
                { symbol: 'AAA', weight: 0.2, forecast: stockFigures },
            ],
        });

        const notices = riskNotices(forecast);

        expect(notices.map((notice) => [notice.severity, notice.title])).toEqual([
            ['warning', 'Портфель нетиповий для моделі'],
            ['info', 'Нетипова акція'],
            ['info', 'Не для всіх акцій є окремий прогноз'],
        ]);
        expect(notices[0].text).toContain('волатильність за місяць');
        expect(notices[1].text).toMatch(/^GME:/);
        expect(notices[2].text).toMatch(/^NEW:/);
    });
});

describe('the axis', () => {
    test('ends at a round number past every value, bound and past value', () => {
        expect(riskScaleMax([portfolioFigures.volatility])).toBe(0.3);
        expect(riskScaleMax([portfolioFigures.volatility, stockFigures.volatility])).toBe(0.4);
        expect(riskScaleMax([portfolioFigures.valueAtRisk95])).toBeCloseTo(0.03);
        expect(riskScaleMax([null, undefined])).toBe(1);
    });

    test('positions stay inside it', () => {
        expect(positionOn(0.15, 0.3)).toBeCloseTo(50);
        expect(positionOn(0.6, 0.3)).toBe(100);
        expect(positionOn(-1, 0.3)).toBe(0);
    });
});

describe('compareRisk', () => {
    test('less risk is better, and what looks the same is the same', () => {
        expect(compareRisk('volatility', 0.2, 0.15)).toBe('better');
        expect(compareRisk('maxDrawdown', 0.1, 0.12)).toBe('worse');
        expect(compareRisk('valueAtRisk95', 0.020001, 0.020002)).toBe('same');
        expect(compareRisk('volatility', null, 0.15)).toBeNull();
    });
});
