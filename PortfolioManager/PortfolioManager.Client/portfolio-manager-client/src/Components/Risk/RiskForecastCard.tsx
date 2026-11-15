import { ReactNode } from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Skeleton from '@mui/material/Skeleton';
import Typography from '@mui/material/Typography';
import ChevronRight from '@mui/icons-material/ChevronRight';
import Insights from '@mui/icons-material/Insights';
import { Link as RouterLink } from 'react-router-dom';
import { intervalMeaning } from '../../Domain/risk';
import { formatDate } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { GetRiskForecast } from '../../Services/StatisticService';
import { getErrorMessage } from '../../Services/errors';
import EmptyState from '../Shared/EmptyState';
import ErrorState from '../Shared/ErrorState';
import { RiskLegend, RiskSummary } from './RiskFigures';
import RiskNotices from './RiskNotices';

interface RiskForecastCardProps {
    portfolioId: number;
    hasPositions: boolean;
    /** Changes after every edit of the portfolio, so the forecast is made again for the new weights. */
    version: number;
}

/** «Ризик на 3 місяці» on the portfolio page: the forecast of the trained model for the whole portfolio. */
export default function RiskForecastCard({ portfolioId, hasPositions, version }: RiskForecastCardProps) {
    // The API refuses a forecast of an empty portfolio, so it is not even asked.
    const forecast = useAsyncData(
        () => (hasPositions ? GetRiskForecast(portfolioId) : Promise.resolve(null)),
        [portfolioId, version, hasPositions]
    );
    const data = forecast.data;

    let body: ReactNode;
    if (!hasPositions) {
        body = (
            <EmptyState
                icon={<Insights />}
                title="Прогноз з’явиться разом із позиціями"
                description="Модель оцінить волатильність, VaR і просадку портфеля на наступні три місяці."
            />
        );
    } else if (forecast.loading) {
        body = (
            <Box role="status" aria-label="Розраховуємо прогноз ризику" sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(3, minmax(0, 1fr))' }, gap: 2 }}>
                    {[0, 1, 2].map((index) => (
                        <Skeleton key={index} variant="rounded" height={148} />
                    ))}
                </Box>
                <Typography variant="caption" color="text.disabled" sx={{ textAlign: 'center' }}>
                    Рахуємо прогноз: модель переглядає історію цін за 10 років, це може тривати до хвилини…
                </Typography>
            </Box>
        );
    } else if (!data) {
        body = (
            <ErrorState
                compact
                title="Не вдалося розрахувати прогноз"
                message={getErrorMessage(
                    forecast.error,
                    'Сервіс розрахунків зараз не відповідає. Спробуйте ще раз за хвилину.'
                )}
                onRetry={forecast.reload}
            />
        );
    } else {
        body = (
            <>
                <RiskNotices forecast={data} />
                <RiskSummary figures={data.portfolio} losses={data.losses} />
                <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: 1 }}>
                    <RiskLegend intervalLevel={data.model.intervalLevel} />
                    <Typography variant="caption" color="text.disabled">
                        Інтервал {Math.round(data.model.intervalLevel * 100)}%: {intervalMeaning(data.model)}. Це не інвестиційна порада.
                    </Typography>
                </Box>
            </>
        );
    }

    return (
        <Card component="section" aria-labelledby="risk-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-start', justifyContent: 'space-between', gap: 1.5 }}>
                <Box>
                    <Typography id="risk-title" variant="h2">Ризик на 3 місяці</Typography>
                    <Typography variant="body2" color="text.secondary">
                        {data
                            ? `Прогноз моделі машинного навчання ${data.model.version} за цінами на ${formatDate(data.asOf)}`
                            : 'Прогноз моделі машинного навчання на наступні 63 торгові дні'}
                    </Typography>
                </Box>
                {data && (
                    <Button component={RouterLink} to={`/portfolios/${portfolioId}/risk`} endIcon={<ChevronRight />} size="small">
                        Детальніше
                    </Button>
                )}
            </Box>
            {body}
        </Card>
    );
}
