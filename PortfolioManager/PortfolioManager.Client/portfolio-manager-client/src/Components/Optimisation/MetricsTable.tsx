import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import IconButton from '@mui/material/IconButton';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import { useTheme } from '@mui/material/styles';
import CheckCircleOutline from '@mui/icons-material/CheckCircleOutline';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import RemoveCircleOutline from '@mui/icons-material/RemoveCircleOutline';
import TrendingFlat from '@mui/icons-material/TrendingFlat';
import { compareMetric, formatMetricChange, formatMetricValue, metricInfo, Verdict } from '../../Domain/metrics';

export interface MetricComparison {
    metricName: string;
    strategyBaseValue: number;
    strategyOptimizedValue: number;
}

const LABELS: Record<Verdict, string> = { better: 'краще', worse: 'гірше', same: 'без змін' };

function VerdictBadge({ verdict }: { verdict: Verdict }) {
    const { app } = useTheme();
    const color = { better: app.gain, worse: app.loss, same: app.flat }[verdict];
    const Icon = { better: CheckCircleOutline, worse: RemoveCircleOutline, same: TrendingFlat }[verdict];
    return (
        <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, color, fontSize: '0.8125rem', fontWeight: 600 }}>
            <Icon aria-hidden sx={{ fontSize: 16 }} />
            {LABELS[verdict]}
        </Box>
    );
}

/** How many metrics improved: shown above the table as the one-line answer. */
export function countVerdicts(comparisons: MetricComparison[]) {
    const verdicts = comparisons.map((item) => compareMetric(item.metricName, item.strategyBaseValue, item.strategyOptimizedValue));
    return { better: verdicts.filter((v) => v === 'better').length, total: verdicts.length };
}

export default function MetricsTable({ comparisons }: { comparisons: MetricComparison[] }) {
    return (
        <Card component="section" aria-labelledby="metrics-title" sx={{ minWidth: 0 }}>
            <Box sx={{ px: 2.5, pt: 2.5, pb: 2 }}>
                <Typography id="metrics-title" variant="h2">Порівняння показників</Typography>
                <Typography variant="body2" color="text.secondary">
                    Поточний портфель проти оптимізованого за однаковий історичний період
                </Typography>
            </Box>
            <TableContainer>
                <Table aria-label="Порівняння показників" sx={{ minWidth: 640 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ pl: 2.5 }}>Показник</TableCell>
                            <TableCell align="right">Поточний портфель</TableCell>
                            <TableCell align="right">Оптимізований портфель</TableCell>
                            <TableCell align="right">Зміна</TableCell>
                            <TableCell sx={{ pr: 2.5 }}>Висновок</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {comparisons.map((item) => {
                            const info = metricInfo(item.metricName);
                            const verdict = compareMetric(item.metricName, item.strategyBaseValue, item.strategyOptimizedValue);
                            return (
                                <TableRow key={item.metricName}>
                                    <TableCell sx={{ pl: 2.5 }}>
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                            <span>{item.metricName}</span>
                                            {info.description && (
                                                <Tooltip title={info.description} enterTouchDelay={0}>
                                                    <IconButton size="small" aria-label={`Що означає «${item.metricName}»`} sx={{ color: 'text.disabled', p: 0.25 }}>
                                                        <InfoOutlined sx={{ fontSize: 16 }} />
                                                    </IconButton>
                                                </Tooltip>
                                            )}
                                        </Box>
                                    </TableCell>
                                    <TableCell align="right" sx={{ color: 'text.secondary' }}>
                                        {formatMetricValue(item.metricName, item.strategyBaseValue)}
                                    </TableCell>
                                    <TableCell align="right" sx={{ fontWeight: 600 }}>
                                        {formatMetricValue(item.metricName, item.strategyOptimizedValue)}
                                    </TableCell>
                                    <TableCell align="right" sx={{ color: 'text.secondary' }}>
                                        {formatMetricChange(item.metricName, item.strategyBaseValue, item.strategyOptimizedValue)}
                                    </TableCell>
                                    <TableCell sx={{ pr: 2.5 }}>
                                        <VerdictBadge verdict={verdict} />
                                    </TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>
        </Card>
    );
}
