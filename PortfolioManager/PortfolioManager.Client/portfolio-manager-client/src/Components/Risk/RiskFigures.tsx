import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import { alpha, useTheme } from '@mui/material/styles';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import { MoneyRangeDto, RiskFiguresDto, RiskMetricDto } from '../../DTO/RiskForecastDto';
import { formatRisk, formatRiskInterval, positionOn, RISK_METRICS, RiskMetricInfo, RiskMetricKey, riskScaleMax } from '../../Domain/risk';
import { formatMoney } from '../../Format/format';
import { useChartColors } from '../../Theme/useChartColors';

/**
 * The pieces every risk view is built from: one metric drawn along an axis (forecast, its interval and the last three
 * months), the legend for it, and the three metrics of a portfolio side by side.
 */

/** The interval band is a wash of the mark's colour; on the dark surface it needs more of it to be seen. */
function useBandAlpha() {
    return useTheme().palette.mode === 'dark' ? 0.4 : 0.24;
}

const isNumber = (value: number | null | undefined): value is number => value !== null && value !== undefined && Number.isFinite(value);

/** «Прогноз 19,1%, інтервал 13,7–25,2%, за останні 3 місяці 17,1%»: what the figure shows, for a screen reader and a tooltip. */
export function describeMetric(key: RiskMetricKey, metric: RiskMetricDto): string {
    const parts = [`прогноз ${formatRisk(key, metric.value)}`];
    if (isNumber(metric.low) && isNumber(metric.high)) parts.push(`інтервал ${formatRiskInterval(key, metric)}`);
    if (isNumber(metric.previous)) parts.push(`за останні 3 місяці ${formatRisk(key, metric.previous)}`);
    return parts.join(', ');
}

interface RiskRangeProps {
    metric: RiskMetricDto;
    /** The value at the right end; views that compare several ranges pass the same one. */
    max: number;
    /** What the figure shows, read out instead of it. */
    label: string;
    emphasis?: boolean;
}

/** One metric along an axis from 0: the 80% interval as a band, the forecast as a dot, the last three months as a tick. */
export function RiskRange({ metric, max, label, emphasis = true }: RiskRangeProps) {
    const colors = useChartColors();
    const bandAlpha = useBandAlpha();
    const at = (value: number) => `${positionOn(value, max)}%`;
    const markColor = emphasis ? colors.accent : colors.neutral;

    return (
        <Box role="img" aria-label={label} sx={{ position: 'relative', height: 20, minWidth: 0 }}>
            <Box sx={{ position: 'absolute', left: 0, right: 0, top: '50%', height: '1px', background: colors.grid }} />
            {isNumber(metric.low) && isNumber(metric.high) && (
                <Box
                    data-mark="interval"
                    sx={{
                        position: 'absolute',
                        left: at(metric.low),
                        width: `${positionOn(metric.high, max) - positionOn(metric.low, max)}%`,
                        top: '50%',
                        height: 8,
                        mt: '-4px',
                        borderRadius: '4px',
                        background: alpha(markColor, bandAlpha),
                    }}
                />
            )}
            {isNumber(metric.previous) && (
                <Box
                    data-mark="previous"
                    sx={{ position: 'absolute', left: at(metric.previous), top: 2, bottom: 2, width: 2, ml: '-1px', borderRadius: 1, background: colors.reference }}
                />
            )}
            {isNumber(metric.value) && (
                <Box
                    data-mark="forecast"
                    sx={{
                        position: 'absolute',
                        left: at(metric.value),
                        top: '50%',
                        width: 10,
                        height: 10,
                        ml: '-5px',
                        mt: '-5px',
                        borderRadius: '50%',
                        background: markColor,
                        boxShadow: `0 0 0 2px ${colors.surface}`,
                    }}
                />
            )}
        </Box>
    );
}

/** Says which mark is which: shape and colour together, so colour is never the only clue. */
export function RiskLegend({ intervalLevel }: { intervalLevel: number }) {
    const colors = useChartColors();
    const bandAlpha = useBandAlpha();
    const item = { display: 'inline-flex', alignItems: 'center', gap: 0.75 };
    return (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', columnGap: 2.5, rowGap: 0.5, fontSize: '0.75rem', color: 'text.secondary' }}>
            <Box component="span" sx={item}>
                <Box component="span" aria-hidden sx={{ width: 10, height: 10, borderRadius: '50%', background: colors.accent }} />
                Прогноз
            </Box>
            <Box component="span" sx={item}>
                <Box component="span" aria-hidden sx={{ width: 18, height: 8, borderRadius: '4px', background: alpha(colors.accent, bandAlpha) }} />
                Інтервал {Math.round(intervalLevel * 100)}%
            </Box>
            <Box component="span" sx={item}>
                <Box component="span" aria-hidden sx={{ width: 2, height: 14, borderRadius: 1, background: colors.reference }} />
                Останні 3 місяці
            </Box>
        </Box>
    );
}

/** The metric's name with its explanation behind an info button; a heading over a panel, plain text in a table. */
export function MetricLabel({ info, heading = true }: { info: RiskMetricInfo; heading?: boolean }) {
    return (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, minWidth: 0 }}>
            {heading ? (
                <Typography component="h3" variant="subtitle2" color="text.secondary">
                    {info.label}
                </Typography>
            ) : (
                <span>{info.label}</span>
            )}
            <Tooltip title={info.description} enterTouchDelay={0}>
                <IconButton size="small" aria-label={`Що означає «${info.label}»`} sx={{ color: 'text.disabled', p: 0.25 }}>
                    <InfoOutlined sx={{ fontSize: 16 }} />
                </IconButton>
            </Tooltip>
        </Box>
    );
}

interface RiskSummaryProps {
    figures: RiskFiguresDto;
    /** The VaR and drawdown in money, when the figures are of a real portfolio. */
    losses?: { valueAtRisk95: MoneyRangeDto; maxDrawdown: MoneyRangeDto };
}

/** The three metrics of a portfolio side by side: the forecast in large type, its interval, and where it stands. */
export function RiskSummary({ figures, losses }: RiskSummaryProps) {
    return (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'repeat(3, minmax(0, 1fr))' }, gap: { xs: 1.5, sm: 2 } }}>
            {RISK_METRICS.map((info) => {
                const metric = figures[info.key];
                const loss = info.inMoney && losses ? losses[info.key as 'valueAtRisk95' | 'maxDrawdown'] : undefined;
                return (
                    <Box
                        key={info.key}
                        component="section"
                        aria-label={info.label}
                        sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2, display: 'flex', flexDirection: 'column', gap: 1, minWidth: 0 }}
                    >
                        <MetricLabel info={info} />
                        <Box sx={{ display: 'flex', alignItems: 'baseline', flexWrap: 'wrap', columnGap: 1 }}>
                            <Typography variant="kpi" component="p" sx={{ m: 0, fontSize: { xs: '1.25rem', sm: '1.75rem' }, lineHeight: { xs: '1.75rem', sm: '2.25rem' } }}>
                                {formatRisk(info.key, metric.value)}
                            </Typography>
                            {loss && isNumber(loss.value) && (
                                <Typography color="text.secondary" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                    ≈ {formatMoney(loss.value)}
                                </Typography>
                            )}
                        </Box>
                        <RiskRange metric={metric} max={riskScaleMax([metric])} label={`${info.label}: ${describeMetric(info.key, metric)}`} />
                        <Typography variant="caption" color="text.secondary" component="p" sx={{ m: 0, fontVariantNumeric: 'tabular-nums' }}>
                            Інтервал: {formatRiskInterval(info.key, metric)}
                            {loss && isNumber(loss.low) && isNumber(loss.high) && ` (${formatMoney(loss.low)}–${formatMoney(loss.high)})`}
                            <br />
                            За останні 3 місяці: {formatRisk(info.key, metric.previous)}
                        </Typography>
                    </Box>
                );
            })}
        </Box>
    );
}
