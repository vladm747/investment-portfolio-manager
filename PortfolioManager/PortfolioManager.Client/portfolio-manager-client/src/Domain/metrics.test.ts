import { compareMetric, formatMetricChange, formatMetricValue, METRICS, metricInfo, toWeightRows } from './metrics';

const plain = (text: string) => text.replace(/[  ]/g, ' ');

describe('compareMetric', () => {
    test('a higher ratio is better', () => {
        expect(compareMetric('sharpe', 1.18, 1.46)).toBe('better');
        expect(compareMetric('sharpe', 1.46, 1.18)).toBe('worse');
    });

    test('lower volatility is better', () => {
        expect(compareMetric('volatility', 0.2196, 0.1904)).toBe('better');
        expect(compareMetric('volatility', 0.19, 0.22)).toBe('worse');
    });

    test('a drawdown is compared by size: −28% is better than −30%', () => {
        expect(compareMetric('maxDrawdown', -0.3, -0.28)).toBe('better');
        expect(compareMetric('maxDrawdown', -0.2847, -0.2961)).toBe('worse');
    });

    test('values that look the same on screen are the same', () => {
        expect(compareMetric('cumulativeReturn', 2.96, 2.96)).toBe('same');
        expect(compareMetric('cumulativeReturn', 2.96001, 2.96002)).toBe('same');
    });

    test('a metric the map does not know is judged as higher is better', () => {
        expect(compareMetric('newMetric', 1, 2)).toBe('better');
    });
});

describe('formatting', () => {
    test('percentages come from fractions, ratios stay plain numbers', () => {
        expect(formatMetricValue('cumulativeReturn', 2.96)).toBe('296,00%');
        expect(formatMetricValue('maxDrawdown', -0.2847)).toBe('−28,47%');
        expect(formatMetricValue('sharpe', 1.4567)).toBe('1,46');
    });

    test('the change of a percentage is in percentage points', () => {
        expect(plain(formatMetricChange('cumulativeReturn', 2.96, 3.84))).toBe('+88,00 п.п.');
        expect(plain(formatMetricChange('volatility', 0.2196, 0.1904))).toBe('−2,92 п.п.');
    });

    test('the change of a ratio is a signed number', () => {
        expect(formatMetricChange('sharpe', 1.18, 1.46)).toBe('+0,28');
        expect(formatMetricChange('sharpe', 1.46, 1.18)).toBe('−0,28');
        expect(formatMetricChange('sharpe', 1.2, 1.2)).toBe('0,00');
    });
});

test('every metric the API sends, by its key, has a rule and a description', () => {
    // The nine keys of PortfolioComparer in the API.
    const keys = [
        'cumulativeReturn', 'cagr', 'sharpe', 'sortino', 'calmar', 'maxDrawdown', 'volatility', 'expectedMonthly', 'expectedYearly',
    ];
    keys.forEach((key) => {
        expect(METRICS[key]).toBeDefined();
        expect(metricInfo(key).description.length).toBeGreaterThan(10);
    });
    expect(Object.keys(METRICS).sort()).toEqual([...keys].sort());
});

test('a metric is found by its key, never by its Ukrainian name', () => {
    expect(metricInfo('Коефіцієнт Шарпа')).not.toBe(METRICS.sharpe);
    expect(metricInfo('sharpe')).toBe(METRICS.sharpe);
});

test('weights are lined up by ticker and shown in percent', () => {
    const rows = toWeightRows([0.5, 0.3, 0.2], { AAPL: 0.7, MSFT: 0, NVDA: 0.3 });
    expect(rows).toEqual([
        { symbol: 'AAPL', current: 50, optimised: 70 },
        { symbol: 'MSFT', current: 30, optimised: 0 },
        { symbol: 'NVDA', current: 20, optimised: 30 },
    ]);
});

test('good direction follows the sign convention of each metric', async () => {
    const { goodDirection } = await import('./metrics');
    expect(goodDirection('sharpe')).toBe('up');
    expect(goodDirection('volatility')).toBe('down');
    expect(goodDirection('maxDrawdown')).toBe('up');
});
