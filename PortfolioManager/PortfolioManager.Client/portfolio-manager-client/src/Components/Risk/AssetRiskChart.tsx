import { useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import WarningAmber from '@mui/icons-material/WarningAmber';
import RiskForecastDto, { RiskFiguresDto } from '../../DTO/RiskForecastDto';
import { formatRisk, formatRiskInterval, outOfDomainReason, positionOn, RISK_AXIS_TICKS, RISK_METRICS, riskMetricInfo, RiskMetricKey, riskScaleMax } from '../../Domain/risk';
import { formatPercent } from '../../Format/format';
import { niceScale } from '../../Domain/niceScale';
import { useChartColors } from '../../Theme/useChartColors';
import { describeMetric, RiskLegend, RiskRange } from './RiskFigures';

interface Row {
    key: string;
    name: string;
    /** Share of the portfolio in percent; missing for the portfolio itself. */
    weight?: number;
    figures: RiskFiguresDto | null;
    isPortfolio: boolean;
}

const LABEL_WIDTH = { xs: 72, sm: 132 };
const VALUE_WIDTH = { xs: 48, sm: 64 };
const GAP = { xs: 1, sm: 2 };

/** «20%», or «2,5%» where the step is below a whole percent. */
const tickText = (tick: number) => {
    const percent = tick * 100;
    return formatPercent(percent, { digits: Math.abs(percent - Math.round(percent)) < 1e-9 ? 0 : 1 });
};

function rowsOf(forecast: RiskForecastDto): Row[] {
    return [
        { key: '__portfolio__', name: 'Портфель', figures: forecast.portfolio, isPortfolio: true },
        ...forecast.assets.map((asset) => ({
            key: asset.symbol,
            name: asset.symbol,
            weight: asset.weight * 100,
            figures: asset.forecast,
            isPortfolio: false,
        })),
    ];
}

function RowLabel({ row }: { row: Row }) {
    return (
        <Box sx={{ width: LABEL_WIDTH, flexShrink: 0, minWidth: 0 }}>
            <Typography sx={{ fontWeight: row.isPortfolio ? 600 : 500, display: 'flex', alignItems: 'center', gap: 0.5 }} noWrap>
                {row.name}
                {row.figures?.outOfDomain && (
                    <Tooltip title={`Нетипово для моделі: ${outOfDomainReason(row.figures)}`} enterTouchDelay={0}>
                        <WarningAmber aria-label="нетипово для моделі" sx={{ fontSize: 16, color: 'warning.main' }} />
                    </Tooltip>
                )}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap component="div">
                {formatPercent(row.weight ?? 100, { digits: 1 })}
            </Typography>
        </Box>
    );
}

/**
 * One metric for the portfolio and each stock on a shared axis, so their risks can be compared: the portfolio in the
 * accent colour, the stocks in grey. The table under it holds every figure of every metric.
 */
export default function AssetRiskChart({ forecast }: { forecast: RiskForecastDto }) {
    const colors = useChartColors();
    const [metricKey, setMetricKey] = useState<RiskMetricKey>('volatility');
    const info = riskMetricInfo(metricKey);
    const rows = useMemo(() => rowsOf(forecast), [forecast]);

    const max = riskScaleMax(rows.map((row) => row.figures?.[metricKey]));
    const ticks = niceScale(0, max, RISK_AXIS_TICKS).ticks.filter((tick) => tick <= max + 1e-12);

    return (
        <Card component="section" aria-labelledby="assets-risk-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-start', justifyContent: 'space-between', gap: 1.5 }}>
                <Box>
                    <Typography id="assets-risk-title" variant="h2">Ризик кожної акції</Typography>
                    <Typography variant="body2" color="text.secondary">
                        Портфель виділено кольором, акції сірим: диверсифікований портфель зазвичай ризикує менше, ніж його акції окремо
                    </Typography>
                </Box>
                <ToggleButtonGroup
                    exclusive
                    size="small"
                    value={metricKey}
                    onChange={(_, next: RiskMetricKey | null) => next && setMetricKey(next)}
                    aria-label="Показник"
                >
                    {RISK_METRICS.map((metric) => (
                        <ToggleButton key={metric.key} value={metric.key}>
                            {metric.shortLabel}
                        </ToggleButton>
                    ))}
                </ToggleButtonGroup>
            </Box>

            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                {rows.map((row) => {
                    const metric = row.figures?.[metricKey];
                    const description = metric ? `${row.name}: ${describeMetric(metricKey, metric)}` : `${row.name}: окремого прогнозу немає`;
                    return (
                        <Box key={row.key} sx={{ display: 'flex', alignItems: 'center', gap: GAP, minWidth: 0, pb: row.isPortfolio ? 1 : 0, borderBottom: row.isPortfolio ? 1 : 0, borderColor: 'divider' }}>
                            <RowLabel row={row} />
                            <Tooltip title={description} enterTouchDelay={0} placement="top">
                                {/* The whole row is the hit target, not the 10px dot; focus shows the same text. */}
                                <Box tabIndex={0} sx={{ flex: 1, minWidth: 0, py: 0.75, borderRadius: 1, '&:hover, &:focus-visible': { background: colors.grid, outline: 'none' } }}>
                                    {metric ? (
                                        <RiskRange metric={metric} max={max} label={`${info.label}, ${description}`} emphasis={row.isPortfolio} />
                                    ) : (
                                        <Typography variant="caption" color="text.disabled">немає окремого прогнозу</Typography>
                                    )}
                                </Box>
                            </Tooltip>
                            <Typography sx={{ width: VALUE_WIDTH, flexShrink: 0, textAlign: 'right', fontWeight: row.isPortfolio ? 600 : 400, fontVariantNumeric: 'tabular-nums' }}>
                                {formatRisk(metricKey, metric?.value)}
                            </Typography>
                        </Box>
                    );
                })}
                {/* The axis: the same scale as every row above. */}
                <Box aria-hidden sx={{ display: 'flex', gap: GAP }}>
                    <Box sx={{ width: LABEL_WIDTH, flexShrink: 0 }} />
                    <Box sx={{ flex: 1, position: 'relative', height: 18, borderTop: 1, borderColor: colors.axis }}>
                        {ticks.map((tick) => (
                            <Typography
                                key={tick}
                                variant="caption"
                                sx={{ position: 'absolute', left: `${positionOn(tick, max)}%`, transform: 'translateX(-50%)', color: colors.mutedText, fontVariantNumeric: 'tabular-nums', whiteSpace: 'nowrap' }}
                            >
                                {tickText(tick)}
                            </Typography>
                        ))}
                    </Box>
                    <Box sx={{ width: VALUE_WIDTH, flexShrink: 0 }} />
                </Box>
            </Box>
            <RiskLegend intervalLevel={forecast.model.intervalLevel} />

            <TableContainer sx={{ mx: -2.5, width: 'auto' }}>
                <Table aria-label="Прогноз ризику акцій у таблиці" size="small" sx={{ minWidth: 640 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ pl: 2.5 }}>Акція</TableCell>
                            <TableCell align="right">Частка</TableCell>
                            {RISK_METRICS.map((metric) => (
                                <TableCell key={metric.key} align="right">{metric.label}</TableCell>
                            ))}
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.map((row) => (
                            <TableRow key={row.key}>
                                <TableCell sx={{ pl: 2.5, fontWeight: row.isPortfolio ? 600 : 500 }}>{row.name}</TableCell>
                                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                    {row.weight === undefined ? formatPercent(100, { digits: 1 }) : formatPercent(row.weight, { digits: 1 })}
                                </TableCell>
                                {RISK_METRICS.map((metric) => {
                                    const figures = row.figures?.[metric.key];
                                    return (
                                        <TableCell key={metric.key} align="right" sx={{ fontVariantNumeric: 'tabular-nums', verticalAlign: 'top' }}>
                                            {figures ? (
                                                <>
                                                    <Box component="span" sx={{ fontWeight: 600 }}>{formatRisk(metric.key, figures.value)}</Box>
                                                    <Typography variant="caption" color="text.secondary" component="div">
                                                        {formatRiskInterval(metric.key, figures)} · було {formatRisk(metric.key, figures.previous)}
                                                    </Typography>
                                                </>
                                            ) : (
                                                <Typography variant="caption" color="text.disabled">немає прогнозу</Typography>
                                            )}
                                        </TableCell>
                                    );
                                })}
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
            <Typography variant="caption" color="text.secondary">
                Під прогнозом: інтервал {Math.round(forecast.model.intervalLevel * 100)}% і значення за останні 3 місяці («було»).
            </Typography>
        </Card>
    );
}
