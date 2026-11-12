import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CircularProgress from '@mui/material/CircularProgress';
import LinearProgress from '@mui/material/LinearProgress';
import Typography from '@mui/material/Typography';
import ArrowBack from '@mui/icons-material/ArrowBack';
import Tune from '@mui/icons-material/Tune';
import { Link as RouterLink, useParams } from 'react-router-dom';
import { formatMetricValue, goodDirection, metricInfo } from '../../Domain/metrics';
import { formatNumber } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { usePageTitle } from '../../Hooks/usePageTitle';
import { GetPortfolioAsync } from '../../Services/PortfolioService';
import { GetOptimisationData } from '../../Services/StatisticService';
import { getErrorMessage } from '../../Services/errors';
import Delta from '../Shared/Delta';
import EmptyState from '../Shared/EmptyState';
import ErrorState from '../Shared/ErrorState';
import KpiCard, { KpiGrid } from '../Shared/KpiCard';
import PageHeader from '../Shared/PageHeader';
import PortfolioNotFound from '../Shared/PortfolioNotFound';
import MetricsTable, { countVerdicts, MetricComparison } from './MetricsTable';
import WeightsChart from './WeightsChart';

interface OptimisationResult {
    comparisons: MetricComparison[];
    weights: {
        baseWeights: number[];
        optimizedWeights: Record<string, number>;
    };
}

/** The four figures that tell the story at a glance; the table below has all of them. */
const HEADLINE_METRICS: { key: string; label: string }[] = [
    { key: 'sharpe', label: 'Коефіцієнт Шарпа' },
    { key: 'cagr', label: 'Річна дохідність (CAGR)' },
    { key: 'volatility', label: 'Волатильність за рік' },
    { key: 'maxDrawdown', label: 'Максимальна просадка' },
];

function Calculating() {
    return (
        <Card role="status" aria-live="polite" sx={{ p: { xs: 3, md: 6 }, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, textAlign: 'center' }}>
            <CircularProgress size={36} aria-label="Розрахунок" />
            <Typography variant="h2">Розраховуємо оптимальні ваги…</Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 480 }}>
                Для цього потрібно завантажити історію цін усіх акцій портфеля, тож розрахунок може тривати до хвилини. Не закривайте сторінку.
            </Typography>
            <LinearProgress sx={{ width: '100%', maxWidth: 320, borderRadius: 1 }} />
        </Card>
    );
}

/** Compares the portfolio as it is with the one the Markowitz optimisation proposes. */
export default function OptimisationPage() {
    const { id } = useParams<{ id: string }>();
    const portfolioId = Number(id);

    if (!Number.isInteger(portfolioId) || portfolioId <= 0) {
        return <PortfolioNotFound />;
    }
    return <OptimisationContent portfolioId={portfolioId} />;
}

function OptimisationContent({ portfolioId }: { portfolioId: number }) {
    const portfolio = useAsyncData(() => GetPortfolioAsync(portfolioId), [portfolioId]);
    const result = useAsyncData<OptimisationResult>(() => GetOptimisationData(portfolioId), [portfolioId]);

    const name = portfolio.data?.name;
    usePageTitle(name ? `Оптимізація · ${name}` : 'Оптимізація');

    const backToPortfolio = (
        <Button variant="outlined" startIcon={<ArrowBack />} component={RouterLink} to={`/portfolios/${portfolioId}`}>
            До портфеля
        </Button>
    );

    const header = (
        <PageHeader
            title="Оптимізація портфеля"
            subtitle={name ? `${name}: порівняння поточних і оптимізованих ваг за моделлю Марковіца` : 'Порівняння поточних і оптимізованих ваг за моделлю Марковіца'}
            breadcrumbs={[
                { label: 'Портфелі', to: '/' },
                { label: name ?? 'Портфель', to: `/portfolios/${portfolioId}` },
                { label: 'Оптимізація' },
            ]}
            actions={backToPortfolio}
        />
    );

    if (result.loading) {
        return (
            <>
                {header}
                <Calculating />
            </>
        );
    }

    const data = result.data;
    if (!data) {
        return (
            <>
                {header}
                <Card>
                    <ErrorState
                        title="Не вдалося виконати оптимізацію"
                        message={getErrorMessage(
                            result.error,
                            'Сервіс розрахунків не відповів: можливо, він недоступний, або в портфелі немає позицій. Спробуйте ще раз за хвилину.'
                        )}
                        onRetry={result.reload}
                    />
                </Card>
            </>
        );
    }

    if (data.comparisons.length === 0) {
        return (
            <>
                {header}
                <Card>
                    <EmptyState icon={<Tune />} title="Немає даних для порівняння" description="Додайте до портфеля позиції й повторіть оптимізацію." />
                </Card>
            </>
        );
    }

    const find = (metricKey: string) => data.comparisons.find((item) => item.metricKey === metricKey);
    const { better, total } = countVerdicts(data.comparisons);

    return (
        <>
            {header}

            <Alert severity="info" icon={false} sx={{ alignItems: 'flex-start' }}>
                <Typography variant="body2" component="div">
                    <strong>Результати розраховано на історичних даних (in-sample).</strong> Оптимізація підбирає ваги за вже відомою історією цін, тому в майбутньому
                    портфель може показати інші результати. Це не інвестиційна порада.
                </Typography>
            </Alert>

            <KpiGrid>
                {HEADLINE_METRICS.map(({ key: metricKey, label }) => {
                    const item = find(metricKey);
                    if (!item) return null;
                    const kind = metricInfo(metricKey).kind;
                    const difference = item.strategyOptimizedValue - item.strategyBaseValue;
                    return (
                        <KpiCard
                            key={metricKey}
                            label={label}
                            value={formatMetricValue(metricKey, item.strategyOptimizedValue)}
                            delta={
                                <Delta
                                    format={kind === 'percent' ? 'points' : 'number'}
                                    value={kind === 'percent' ? difference * 100 : difference}
                                    good={goodDirection(metricKey)}
                                />
                            }
                            note={`було ${formatMetricValue(metricKey, item.strategyBaseValue)}`}
                        />
                    );
                })}
            </KpiGrid>

            <Box sx={{ color: 'text.secondary' }}>
                <Typography>
                    Оптимізований портфель кращий за {formatNumber(better)} з {formatNumber(total)} показників.
                </Typography>
            </Box>

            <WeightsChart baseWeights={data.weights.baseWeights} optimisedWeights={data.weights.optimizedWeights} />
            <MetricsTable comparisons={data.comparisons} />
        </>
    );
}
