import { useMemo } from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, TooltipProps, XAxis, YAxis } from 'recharts';
import { toWeightRows, WeightRow } from '../../Domain/metrics';
import { niceScale } from '../../Domain/niceScale';
import { formatPercent } from '../../Format/format';
import { useChartColors } from '../../Theme/useChartColors';
import Delta from '../Shared/Delta';

interface WeightsChartProps {
    baseWeights: number[];
    optimisedWeights: Record<string, number>;
}

function WeightsTooltip({ active, payload }: TooltipProps<number, string>) {
    if (!active || !payload?.length) {
        return null;
    }
    const row = payload[0].payload as WeightRow;
    return (
        <Card sx={{ p: 1.5, minWidth: 190, boxShadow: (theme) => theme.app.menuShadow }}>
            <Typography sx={{ fontWeight: 600 }}>{row.symbol}</Typography>
            <Typography variant="caption" color="text.secondary" component="div" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                Поточна вага: {formatPercent(row.current, { digits: 1 })}
            </Typography>
            <Typography variant="caption" color="text.secondary" component="div" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                Оптимізована вага: {formatPercent(row.optimised, { digits: 1 })}
            </Typography>
        </Card>
    );
}

/** Current against optimised weight of every ticker: grouped bars, and the same numbers as a table. */
export default function WeightsChart({ baseWeights, optimisedWeights }: WeightsChartProps) {
    const colors = useChartColors();
    const rows = useMemo(() => toWeightRows(baseWeights, optimisedWeights), [baseWeights, optimisedWeights]);

    const scale = useMemo(() => niceScale(0, Math.max(10, ...rows.flatMap((row) => [row.current, row.optimised])), 5), [rows]);

    const summary = `Ваги тікерів, поточні та оптимізовані: ${rows
        .map((row) => `${row.symbol} ${formatPercent(row.current, { digits: 1 })} і ${formatPercent(row.optimised, { digits: 1 })}`)
        .join('; ')}`;

    return (
        <Card component="section" aria-labelledby="weights-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
            <Box>
                <Typography id="weights-title" variant="h2">Ваги акцій у портфелі</Typography>
                <Typography variant="body2" color="text.secondary">Частка кожної акції: зараз і після оптимізації</Typography>
            </Box>

            <Box role="img" aria-label={summary} sx={{ height: 280, width: '100%', '& .recharts-wrapper': { fontFamily: colors.fontFamily } }}>
                <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={rows} margin={{ top: 8, right: 8, left: 0, bottom: 0 }} barGap={2} barCategoryGap="28%">
                        <CartesianGrid vertical={false} stroke={colors.grid} />
                        <XAxis dataKey="symbol" tick={{ fill: colors.text, fontSize: 12, fontWeight: 500 }} tickLine={false} axisLine={{ stroke: colors.axis }} tickMargin={8} />
                        <YAxis
                            tickFormatter={(value: number) => formatPercent(value, { digits: 0 })}
                            tick={{ fill: colors.mutedText, fontSize: 12 }}
                            tickLine={false}
                            axisLine={false}
                            width={48}
                            ticks={scale.ticks}
                            domain={scale.domain}
                        />
                        <Tooltip content={<WeightsTooltip />} cursor={{ fill: colors.grid, fillOpacity: 0.5 }} isAnimationActive={false} />
                        <Bar dataKey="current" name="Поточна вага" fill={colors.neutral} radius={[4, 4, 0, 0]} maxBarSize={22} isAnimationActive={false} />
                        <Bar dataKey="optimised" name="Оптимізована вага" fill={colors.accent} radius={[4, 4, 0, 0]} maxBarSize={22} isAnimationActive={false} />
                    </BarChart>
                </ResponsiveContainer>
            </Box>

            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2.5, fontSize: '0.75rem', color: 'text.secondary' }}>
                <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 1 }}>
                    <Box component="span" sx={{ width: 10, height: 10, borderRadius: '3px', background: colors.neutral }} />
                    Поточна вага
                </Box>
                <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 1 }}>
                    <Box component="span" sx={{ width: 10, height: 10, borderRadius: '3px', background: colors.accent }} />
                    Оптимізована вага
                </Box>
            </Box>

            <TableContainer sx={{ mx: -2.5, width: 'auto' }}>
                <Table size="small" aria-label="Ваги акцій у таблиці">
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ pl: 2.5 }}>Тікер</TableCell>
                            <TableCell align="right">Поточна вага</TableCell>
                            <TableCell align="right">Оптимізована вага</TableCell>
                            <TableCell align="right" sx={{ pr: 2.5 }}>Зміна</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.map((row) => (
                            <TableRow key={row.symbol}>
                                <TableCell sx={{ pl: 2.5, fontWeight: 600 }}>{row.symbol}</TableCell>
                                <TableCell align="right" sx={{ color: 'text.secondary' }}>{formatPercent(row.current, { digits: 1 })}</TableCell>
                                <TableCell align="right" sx={{ fontWeight: 600, color: row.optimised === 0 ? 'text.disabled' : 'text.primary' }}>
                                    {formatPercent(row.optimised, { digits: 1 })}
                                </TableCell>
                                <TableCell align="right" sx={{ pr: 2.5 }}>
                                    <Delta variant="text" format="points" digits={1} good="up" value={row.optimised - row.current} />
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </Card>
    );
}
