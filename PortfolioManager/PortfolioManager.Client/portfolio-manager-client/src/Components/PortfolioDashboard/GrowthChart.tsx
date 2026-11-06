import { ReactNode, useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import Typography from '@mui/material/Typography';
import ShowChart from '@mui/icons-material/ShowChart';
import { Area, CartesianGrid, ComposedChart, Line, ResponsiveContainer, Tooltip, TooltipProps, XAxis, YAxis } from 'recharts';
import StockDto from '../../DTO/StockDto';
import { buildGrowthSeries, GROWTH_RANGES, GrowthPoint, GrowthRange, periodChange, sliceRange } from '../../Domain/growth';
import { niceScale } from '../../Domain/niceScale';
import { formatDateLong, formatDateShort, formatMoney, formatMonthYear } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { GetGrowthStatsData } from '../../Services/StatisticService';
import { ChartColors, useChartColors } from '../../Theme/useChartColors';
import Delta from '../Shared/Delta';
import EmptyState from '../Shared/EmptyState';
import ErrorState from '../Shared/ErrorState';
import { ChartSkeleton } from '../Shared/Skeletons';

const CHART_HEIGHT = 276;

/** Read out after the visible label, so the name always contains the text on the button. */
const RANGE_NAMES: Record<GrowthRange, string> = {
    '1M': '1М: 1 місяць',
    '3M': '3М: 3 місяці',
    '6M': '6М: 6 місяців',
    '1Y': '1Р: 1 рік',
    ALL: 'Усе: весь час',
};

interface GrowthChartProps {
    portfolioId: number;
    stocks: StockDto[];
    /** Changes after every edit of the portfolio, so the chart is fetched again with the new positions. */
    version: number;
}

function GrowthTooltip({ active, payload, colors }: TooltipProps<number, string> & { colors: ChartColors }) {
    if (!active || !payload?.length) {
        return null;
    }
    const point = payload[0].payload as GrowthPoint;
    return (
        <Card sx={{ p: 1.5, minWidth: 180, boxShadow: (theme) => theme.app.menuShadow }}>
            <Typography variant="caption" color="text.secondary">{formatDateLong(point.ts)}</Typography>
            <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>{formatMoney(point.value)}</Typography>
            <Typography variant="caption" sx={{ color: colors.mutedText, fontVariantNumeric: 'tabular-nums' }}>
                Вкладено {formatMoney(point.invested)}
            </Typography>
        </Card>
    );
}

export default function GrowthChart({ portfolioId, stocks, version }: GrowthChartProps) {
    const colors = useChartColors();
    const [range, setRange] = useState<GrowthRange>('1Y');
    const hasPositions = stocks.length > 0;

    // With no positions there is nothing to chart, and the math service rejects an empty portfolio.
    const growth = useAsyncData(
        () => (hasPositions ? GetGrowthStatsData(portfolioId) : Promise.resolve(null)),
        [portfolioId, version, hasPositions]
    );

    const all = useMemo(() => buildGrowthSeries(growth.data?.growthData, stocks), [growth.data, stocks]);
    const points = useMemo(() => sliceRange(all, range), [all, range]);
    const change = useMemo(() => periodChange(points), [points]);

    const scale = useMemo(() => {
        const values = points.flatMap((point) => [point.value, point.invested]);
        return niceScale(Math.min(...values), Math.max(...values));
    }, [points]);

    const spanDays = points.length > 1 ? (points[points.length - 1].ts - points[0].ts) / 86_400_000 : 0;
    const formatTick = (ts: number) => (spanDays <= 100 ? formatDateShort(ts) : formatMonthYear(ts));

    const summary =
        points.length > 1
            ? `Вартість портфеля з ${formatDateLong(points[0].ts)} до ${formatDateLong(points[points.length - 1].ts)}: від ${formatMoney(points[0].value)} до ${formatMoney(points[points.length - 1].value)}`
            : 'Недостатньо даних для графіка';

    let body: ReactNode;
    if (!hasPositions) {
        body = (
            <EmptyState
                icon={<ShowChart />}
                title="Поки немає даних для графіка"
                description="Додайте першу позицію, і тут з’явиться динаміка вартості портфеля."
            />
        );
    } else if (growth.loading) {
        body = (
            <>
                <ChartSkeleton height={CHART_HEIGHT} />
                <Typography variant="caption" color="text.disabled" sx={{ textAlign: 'center' }}>
                    Завантажуємо історію цін, це може тривати до хвилини…
                </Typography>
            </>
        );
    } else if (growth.error && all.length === 0) {
        body = (
            <ErrorState
                compact
                title="Не вдалося побудувати графік"
                message="Історію цін не отримано: сервіс розрахунків може бути недоступним. Спробуйте ще раз за хвилину."
                onRetry={growth.reload}
            />
        );
    } else if (points.length < 2) {
        body = <EmptyState icon={<ShowChart />} title="Недостатньо даних для графіка" description="Для обраного періоду немає достатньо точок. Спробуйте обрати довший період." />;
    } else {
        body = (
            <>
                <Box role="img" aria-label={summary} sx={{ height: CHART_HEIGHT, width: '100%', '& .recharts-wrapper': { fontFamily: colors.fontFamily } }}>
                    <ResponsiveContainer width="100%" height="100%">
                        <ComposedChart data={points} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                            <CartesianGrid vertical={false} stroke={colors.grid} />
                            <XAxis
                                dataKey="ts"
                                type="number"
                                scale="time"
                                domain={['dataMin', 'dataMax']}
                                tickFormatter={formatTick}
                                tickCount={5}
                                tick={{ fill: colors.mutedText, fontSize: 12 }}
                                tickLine={false}
                                axisLine={{ stroke: colors.axis }}
                                tickMargin={8}
                                minTickGap={48}
                            />
                            <YAxis
                                ticks={scale.ticks}
                                domain={scale.domain}
                                tickFormatter={(value: number) => formatMoney(value, { digits: 0 })}
                                tick={{ fill: colors.mutedText, fontSize: 12 }}
                                tickLine={false}
                                axisLine={false}
                                width={72}
                            />
                            <Tooltip
                                content={<GrowthTooltip colors={colors} />}
                                cursor={{ stroke: colors.mutedText, strokeWidth: 1 }}
                                isAnimationActive={false}
                            />
                            <Area
                                type="monotone"
                                dataKey="value"
                                stroke={colors.accent}
                                strokeWidth={2}
                                fill={colors.accent}
                                fillOpacity={0.12}
                                dot={false}
                                activeDot={{ r: 5, fill: colors.accent, stroke: colors.surface, strokeWidth: 2 }}
                                isAnimationActive={false}
                            />
                            <Line
                                type="stepAfter"
                                dataKey="invested"
                                stroke={colors.mutedText}
                                strokeWidth={1.5}
                                strokeDasharray="5 4"
                                dot={false}
                                activeDot={false}
                                isAnimationActive={false}
                            />
                        </ComposedChart>
                    </ResponsiveContainer>
                </Box>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2.5, fontSize: '0.75rem', color: 'text.secondary' }}>
                    <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 1 }}>
                        <Box component="span" sx={{ width: 16, height: 2, borderRadius: 1, background: colors.accent }} />
                        Вартість портфеля
                    </Box>
                    <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 1 }}>
                        <svg width="16" height="2" aria-hidden>
                            <line x1="0" y1="1" x2="16" y2="1" stroke={colors.mutedText} strokeWidth="1.5" strokeDasharray="5 3" />
                        </svg>
                        Вкладено
                    </Box>
                </Box>
            </>
        );
    }

    const showControls = hasPositions && !growth.loading && all.length > 0;

    return (
        <Card component="section" aria-labelledby="growth-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 1.5, minWidth: 0, height: '100%' }}>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-start', justifyContent: 'space-between', gap: 1.5 }}>
                <Box>
                    <Typography id="growth-title" variant="h2">Динаміка вартості</Typography>
                    {showControls && points.length > 1 && (
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.25, color: 'text.secondary', fontSize: '0.8125rem' }}>
                            <span>За період:</span>
                            <Delta variant="text" showIcon format="money" value={change.change} />
                            <Delta variant="text" format="percent" value={change.percent} />
                        </Box>
                    )}
                </Box>
                {showControls && (
                    <ToggleButtonGroup
                        exclusive
                        size="small"
                        value={range}
                        onChange={(_, next: GrowthRange | null) => next && setRange(next)}
                        aria-label="Період"
                    >
                        {GROWTH_RANGES.map((item) => (
                            <ToggleButton key={item.key} value={item.key} aria-label={RANGE_NAMES[item.key]}>
                                {item.label}
                            </ToggleButton>
                        ))}
                    </ToggleButtonGroup>
                )}
            </Box>
            {body}
        </Card>
    );
}
