import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import { OptimisationRiskForecastDto, RiskMetricDto } from '../../DTO/RiskForecastDto';
import { compareRisk, formatRisk, formatRiskInterval, outOfDomainReason, RISK_METRICS, RiskMetricKey } from '../../Domain/risk';
import { formatDate } from '../../Format/format';
import { MetricLabel } from '../Risk/RiskFigures';
import { VerdictBadge } from './MetricsTable';

function Figure({ metricKey, metric, strong }: { metricKey: RiskMetricKey; metric: RiskMetricDto; strong?: boolean }) {
    return (
        <>
            <Box component="span" sx={{ fontWeight: strong ? 600 : 400, color: strong ? 'text.primary' : 'text.secondary' }}>
                {formatRisk(metricKey, metric.value)}
            </Box>
            <Typography variant="caption" color="text.secondary" component="div">
                {formatRiskInterval(metricKey, metric)}
            </Typography>
        </>
    );
}

/**
 * The risk the trained model expects over the next three months for the current and the optimised weights. Unlike the
 * table above, which measures the past, this looks forward. `forecast` is null when the model could not make it.
 */
export default function RiskComparison({ forecast }: { forecast: OptimisationRiskForecastDto | null }) {
    if (!forecast) {
        return (
            <Typography variant="body2" color="text.secondary">
                Прогноз ризику на 3 місяці для цих ваг зараз недоступний: сервіс розрахунків не зміг його зробити.
            </Typography>
        );
    }

    const unusual = [
        forecast.base.outOfDomain && `поточний портфель: ${outOfDomainReason(forecast.base)}`,
        forecast.optimised.outOfDomain && `оптимізований портфель: ${outOfDomainReason(forecast.optimised)}`,
    ].filter(Boolean);

    return (
        <Card component="section" aria-labelledby="risk-forecast-title" sx={{ minWidth: 0 }}>
            <Box sx={{ px: 2.5, pt: 2.5, pb: 2 }}>
                <Typography id="risk-forecast-title" variant="h2">Прогноз ризику на 3 місяці</Typography>
                <Typography variant="body2" color="text.secondary">
                    Що модель машинного навчання {forecast.model.version} очікує для обох наборів ваг за цінами на {formatDate(forecast.asOf)}.
                    Під значенням: інтервал {Math.round(forecast.model.intervalLevel * 100)}%.
                </Typography>
            </Box>
            <TableContainer>
                <Table aria-label="Прогноз ризику" sx={{ minWidth: 560 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ pl: 2.5 }}>Показник</TableCell>
                            <TableCell align="right">Поточний портфель</TableCell>
                            <TableCell align="right">Оптимізований портфель</TableCell>
                            <TableCell sx={{ pr: 2.5 }}>Висновок</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {RISK_METRICS.map((info) => {
                            const current = forecast.base[info.key];
                            const optimised = forecast.optimised[info.key];
                            const verdict = compareRisk(info.key, current.value, optimised.value);
                            return (
                                <TableRow key={info.key}>
                                    <TableCell sx={{ pl: 2.5 }}>
                                        <MetricLabel info={info} heading={false} />
                                    </TableCell>
                                    <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                        <Figure metricKey={info.key} metric={current} />
                                    </TableCell>
                                    <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                        <Figure metricKey={info.key} metric={optimised} strong />
                                    </TableCell>
                                    <TableCell sx={{ pr: 2.5 }}>{verdict && <VerdictBadge verdict={verdict} />}</TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>
            <Box sx={{ px: 2.5, py: 1.5 }}>
                <Typography variant="caption" color="text.secondary" component="p" sx={{ m: 0 }}>
                    Висновок порівнює самі прогнози. Інтервали показують, у яких межах може опинитися фактичне значення, тож
                    невелика різниця між прогнозами — лише орієнтир.
                </Typography>
                {forecast.model.stale && (
                    <Typography variant="caption" color="text.secondary" component="p" sx={{ m: 0 }}>
                        Модель навчено на цінах до {formatDate(forecast.model.trainedThrough)}; її варто оновити, тож прогноз може бути менш точним.
                    </Typography>
                )}
                {unusual.map((text) => (
                    <Typography key={String(text)} variant="caption" color="text.secondary" component="p" sx={{ m: 0 }}>
                        Нетипово для моделі, {text}
                    </Typography>
                ))}
            </Box>
        </Card>
    );
}
