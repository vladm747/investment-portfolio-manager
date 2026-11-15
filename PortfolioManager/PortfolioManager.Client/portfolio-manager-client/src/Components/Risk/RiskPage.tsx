import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CircularProgress from '@mui/material/CircularProgress';
import LinearProgress from '@mui/material/LinearProgress';
import Typography from '@mui/material/Typography';
import ArrowBack from '@mui/icons-material/ArrowBack';
import { Link as RouterLink, useParams } from 'react-router-dom';
import RiskForecastDto from '../../DTO/RiskForecastDto';
import { horizonText, intervalMeaning } from '../../Domain/risk';
import { formatDate, formatMoney, pluralize } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { usePageTitle } from '../../Hooks/usePageTitle';
import { GetPortfolioAsync } from '../../Services/PortfolioService';
import { GetRiskForecast } from '../../Services/StatisticService';
import { getErrorMessage } from '../../Services/errors';
import ErrorState from '../Shared/ErrorState';
import PageHeader from '../Shared/PageHeader';
import PortfolioNotFound from '../Shared/PortfolioNotFound';
import AssetRiskChart from './AssetRiskChart';
import { RiskLegend, RiskSummary } from './RiskFigures';
import RiskNotices from './RiskNotices';

function Calculating() {
    return (
        <Card role="status" aria-live="polite" sx={{ p: { xs: 3, md: 6 }, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, textAlign: 'center' }}>
            <CircularProgress size={36} aria-label="Розрахунок" />
            <Typography variant="h2">Розраховуємо прогноз ризику…</Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 480 }}>
                Модель переглядає історію цін усіх акцій портфеля, індексу S&amp;P 500 і VIX за 10 років, тож це може тривати до хвилини.
            </Typography>
            <LinearProgress sx={{ width: '100%', maxWidth: 320, borderRadius: 1 }} />
        </Card>
    );
}

/** The facts behind the forecast, for whoever wants to know how far to trust it. */
function ModelFacts({ forecast }: { forecast: RiskForecastDto }) {
    const { model } = forecast;
    const facts: [string, string][] = [
        ['Модель', `${model.version}: градієнтний бустинг (LightGBM), волатильність усереднено з GARCH(1,1)`],
        ['Навчена на цінах до', formatDate(model.trainedThrough)],
        ['Вік моделі', `${pluralize(model.ageDays, { one: 'день', few: 'дні', many: 'днів' })}; оновлювати щонайменше раз на ${model.staleAfterDays} днів`],
        ['Горизонт прогнозу', horizonText(model)],
        ['Ціни на дату', formatDate(forecast.asOf)],
        ['Спільна історія портфеля', pluralize(forecast.tradingDays, { one: 'торговий день', few: 'торгові дні', many: 'торгових днів' })],
        ['Вартість портфеля', formatMoney(forecast.portfolioValue)],
    ];
    return (
        <Card component="section" aria-labelledby="model-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            <Typography id="model-title" variant="h2">Про прогноз</Typography>
            <Box component="dl" sx={{ m: 0, display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'minmax(180px, max-content) 1fr' }, columnGap: 3, rowGap: { xs: 0.25, sm: 1 } }}>
                {facts.map(([term, value]) => (
                    <Box key={term} sx={{ display: 'contents' }}>
                        <Typography component="dt" color="text.secondary" sx={{ mt: { xs: 1, sm: 0 } }}>{term}</Typography>
                        <Typography component="dd" sx={{ m: 0 }}>{value}</Typography>
                    </Box>
                ))}
            </Box>
            <Typography variant="body2" color="text.secondary">
                Модель навчено на щоденних цінах акцій США та синтетичних портфелів із них і перевірено на роках, яких вона не бачила під
                час навчання: там її прогнози волатильності, VaR і просадки точніші за історичні оцінки, якими система користувалася досі.
            </Typography>
        </Card>
    );
}

/** The risk forecast of a portfolio in full: the portfolio, each of its stocks, and what the forecast rests on. */
export default function RiskPage() {
    const { id } = useParams<{ id: string }>();
    const portfolioId = Number(id);

    if (!Number.isInteger(portfolioId) || portfolioId <= 0) {
        return <PortfolioNotFound />;
    }
    return <RiskContent portfolioId={portfolioId} />;
}

function RiskContent({ portfolioId }: { portfolioId: number }) {
    const portfolio = useAsyncData(() => GetPortfolioAsync(portfolioId), [portfolioId]);
    const forecast = useAsyncData(() => GetRiskForecast(portfolioId), [portfolioId]);

    const name = portfolio.data?.name;
    usePageTitle(name ? `Прогноз ризику · ${name}` : 'Прогноз ризику');

    const header = (
        <PageHeader
            title="Прогноз ризику на 3 місяці"
            subtitle={name ? `${name}: волатильність, VaR і просадка за прогнозом моделі машинного навчання` : 'Волатильність, VaR і просадка за прогнозом моделі машинного навчання'}
            breadcrumbs={[
                { label: 'Портфелі', to: '/' },
                { label: name ?? 'Портфель', to: `/portfolios/${portfolioId}` },
                { label: 'Прогноз ризику' },
            ]}
            actions={
                <Button variant="outlined" startIcon={<ArrowBack />} component={RouterLink} to={`/portfolios/${portfolioId}`}>
                    До портфеля
                </Button>
            }
        />
    );

    if (forecast.loading) {
        return (
            <>
                {header}
                <Calculating />
            </>
        );
    }

    const data = forecast.data;
    if (!data) {
        return (
            <>
                {header}
                <Card>
                    <ErrorState
                        title="Не вдалося розрахувати прогноз"
                        message={getErrorMessage(forecast.error, 'Сервіс розрахунків зараз не відповідає. Спробуйте ще раз за хвилину.')}
                        onRetry={forecast.reload}
                    />
                </Card>
            </>
        );
    }

    return (
        <>
            {header}

            <Alert severity="info" icon={false}>
                <Typography variant="body2" component="div">
                    <strong>Це прогноз, а не гарантія.</strong> Інтервал {Math.round(data.model.intervalLevel * 100)}% означає, що {intervalMeaning(data.model)}.
                    Поруч для порівняння показано те саме значення за останні 3 місяці. Це не інвестиційна порада.
                </Typography>
            </Alert>

            <RiskNotices forecast={data} />

            <Card component="section" aria-labelledby="portfolio-risk-title" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Box>
                    <Typography id="portfolio-risk-title" variant="h2">Портфель</Typography>
                    <Typography variant="body2" color="text.secondary">
                        Суми в грошах: від поточної вартості {formatMoney(data.portfolioValue)}
                    </Typography>
                </Box>
                <RiskSummary figures={data.portfolio} losses={data.losses} />
                <RiskLegend intervalLevel={data.model.intervalLevel} />
            </Card>

            {data.assets.length > 0 && <AssetRiskChart forecast={data} />}

            <ModelFacts forecast={data} />
        </>
    );
}
