import { useEffect, useMemo, useRef } from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Typography from '@mui/material/Typography';
import PieChartOutline from '@mui/icons-material/PieChartOutline';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip, TooltipProps } from 'recharts';
import StockDto from '../../DTO/StockDto';
import { sectorColor, sectorName } from '../../Domain/sectors';
import { formatMoney, formatPercent, pluralize } from '../../Format/format';
import { useChartColors } from '../../Theme/useChartColors';
import EmptyState from '../Shared/EmptyState';

interface Slice {
    sector: number;
    name: string;
    value: number;
    share: number;
    color: string;
}

function SliceTooltip({ active, payload }: TooltipProps<number, string>) {
    if (!active || !payload?.length) {
        return null;
    }
    const slice = payload[0].payload as Slice;
    return (
        <Card sx={{ p: 1.5, boxShadow: (theme) => theme.app.menuShadow }}>
            <Typography variant="caption" color="text.secondary">{slice.name}</Typography>
            <Typography sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                {formatMoney(slice.value)} · {formatPercent(slice.share, { digits: 1 })}
            </Typography>
        </Card>
    );
}

/** Where the money is: the share of each sector in the portfolio's current value. */
export default function SectorDonut({ stocks }: { stocks: StockDto[] }) {
    const colors = useChartColors();
    const chartRef = useRef<HTMLDivElement>(null);

    const { slices, total } = useMemo(() => {
        const bySector = new Map<number, number>();
        stocks.forEach((stock) => bySector.set(stock.sector, (bySector.get(stock.sector) ?? 0) + stock.totalCurrentPrice));
        const sum = Array.from(bySector.values()).reduce((a, b) => a + b, 0);
        const list: Slice[] = Array.from(bySector, ([sector, value]) => ({
            sector,
            name: sectorName(sector),
            value,
            share: sum > 0 ? (value / sum) * 100 : 0,
            color: sectorColor(sector, colors.categorical, colors.otherCategory),
        })).sort((a, b) => b.value - a.value);
        return { slices: list, total: sum };
    }, [stocks, colors.categorical, colors.otherCategory]);

    // Recharts marks every slice role="img" without a name, which assistive tech reports as unlabelled images.
    // The wrapper below describes the whole chart, so the slices are made presentational. The slices appear only
    // after the chart has measured its container, so the change is repeated whenever the chart's elements change.
    useEffect(() => {
        const root = chartRef.current;
        if (!root) return;
        const markPresentational = () =>
            root.querySelectorAll('path[role="img"]').forEach((slice) => slice.setAttribute('role', 'presentation'));
        markPresentational();
        const observer = new MutationObserver(markPresentational);
        observer.observe(root, { childList: true, subtree: true });
        return () => observer.disconnect();
    }, []);

    const summary = `Розподіл за секторами: ${slices.map((slice) => `${slice.name} ${formatPercent(slice.share, { digits: 1 })}`).join(', ')}`;

    return (
        <Card component="section" aria-labelledby="sectors-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0, height: '100%' }}>
            <Box>
                <Typography id="sectors-title" variant="h2">Розподіл за секторами</Typography>
                <Typography variant="body2" color="text.secondary">Частка поточної вартості</Typography>
            </Box>

            {slices.length === 0 ? (
                <EmptyState icon={<PieChartOutline />} title="Немає даних" description="Розподіл з’явиться, коли в портфелі будуть позиції." />
            ) : (
                <>
                    <Box ref={chartRef} role="img" aria-label={summary} sx={{ position: 'relative', height: 200, alignSelf: 'center', width: '100%', maxWidth: 240 }}>
                        <ResponsiveContainer width="100%" height="100%">
                            <PieChart>
                                <Pie
                                    data={slices}
                                    dataKey="value"
                                    nameKey="name"
                                    innerRadius="66%"
                                    outerRadius="96%"
                                    paddingAngle={slices.length > 1 ? 2 : 0}
                                    startAngle={90}
                                    endAngle={-270}
                                    stroke="none"
                                    isAnimationActive={false}
                                >
                                    {slices.map((slice) => (
                                        <Cell key={slice.sector} fill={slice.color} />
                                    ))}
                                </Pie>
                                <Tooltip content={<SliceTooltip />} isAnimationActive={false} />
                            </PieChart>
                        </ResponsiveContainer>
                        <Box
                            aria-hidden
                            sx={{ position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', pointerEvents: 'none' }}
                        >
                            <Typography sx={{ fontSize: '1.25rem', lineHeight: '1.75rem', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                                {formatMoney(total, { digits: 0 })}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                                {pluralize(slices.length, { one: 'сектор', few: 'сектори', many: 'секторів' })}
                            </Typography>
                        </Box>
                    </Box>

                    <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0, display: 'flex', flexDirection: 'column', gap: 1.25 }}>
                        {slices.map((slice) => (
                            <Box
                                component="li"
                                key={slice.sector}
                                sx={{ display: 'grid', gridTemplateColumns: '12px minmax(0, 1fr) auto auto', alignItems: 'center', columnGap: 1.25 }}
                            >
                                <Box aria-hidden sx={{ width: 10, height: 10, borderRadius: '3px', background: slice.color }} />
                                <Typography noWrap>{slice.name}</Typography>
                                <Typography variant="body2" color="text.disabled" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                    {formatMoney(slice.value, { digits: 0 })}
                                </Typography>
                                <Typography sx={{ minWidth: 56, textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                                    {formatPercent(slice.share, { digits: 1 })}
                                </Typography>
                            </Box>
                        ))}
                    </Box>
                </>
            )}
        </Card>
    );
}
