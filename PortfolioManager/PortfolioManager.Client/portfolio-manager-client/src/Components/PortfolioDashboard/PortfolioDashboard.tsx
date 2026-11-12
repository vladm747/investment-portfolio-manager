import { useMemo, useState } from 'react';
import axios from 'axios';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CircularProgress from '@mui/material/CircularProgress';
import Skeleton from '@mui/material/Skeleton';
import Add from '@mui/icons-material/Add';
import AccessTime from '@mui/icons-material/AccessTime';
import Refresh from '@mui/icons-material/Refresh';
import Tune from '@mui/icons-material/Tune';
import { Link as RouterLink, useParams } from 'react-router-dom';
import StockDto from '../../DTO/StockDto';
import { summarise } from '../../Domain/summarise';
import { sectorName } from '../../Domain/sectors';
import { getPricesUpdatedAt, setPricesUpdatedAt } from '../../Domain/pricesUpdated';
import { formatDate, formatMoney, formatUpdatedAt, pluralize, positionsCount } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { usePageTitle } from '../../Hooks/usePageTitle';
import { GetPortfolioAsync } from '../../Services/PortfolioService';
import { DeleteStock, GetAllStocksAsync, RefreshPrices } from '../../Services/StockService';
import { getErrorMessage } from '../../Services/errors';
import ConfirmDialog from '../Shared/ConfirmDialog';
import Delta from '../Shared/Delta';
import ErrorState from '../Shared/ErrorState';
import KpiCard, { KpiGrid } from '../Shared/KpiCard';
import { useNotify } from '../Shared/Notifications';
import PageHeader from '../Shared/PageHeader';
import PortfolioNotFound from '../Shared/PortfolioNotFound';
import { ChartSkeleton, KpiRowSkeleton, TableSkeleton } from '../Shared/Skeletons';
import AddPositionDialog from './AddPositionDialog';
import ChangeQuantityDialog from './ChangeQuantityDialog';
import GrowthChart from './GrowthChart';
import PositionsTable from './PositionsTable';
import SectorDonut from './SectorDonut';

function DashboardSkeleton() {
    return (
        <>
            <Skeleton width={280} height={44} />
            <KpiRowSkeleton />
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 2fr) minmax(0, 1fr)' }, gap: 2 }}>
                <Card sx={{ p: 2.5 }}><ChartSkeleton /></Card>
                <Card sx={{ p: 2.5 }}><ChartSkeleton /></Card>
            </Box>
            <Card><TableSkeleton rows={5} columns={6} /></Card>
        </>
    );
}

/** The portfolio is identified by the address, so a reload or a shared link opens the same portfolio. */
const PortfolioDashboard = () => {
    const { id } = useParams<{ id: string }>();
    const portfolioId = Number(id);

    if (!Number.isInteger(portfolioId) || portfolioId <= 0) {
        return <PortfolioNotFound />;
    }

    return <PortfolioDashboardContent portfolioId={portfolioId} />;
};

function PortfolioDashboardContent({ portfolioId }: { portfolioId: number }) {
    const notify = useNotify();
    const portfolio = useAsyncData(() => GetPortfolioAsync(portfolioId), [portfolioId]);
    const stocks = useAsyncData(() => GetAllStocksAsync(portfolioId), [portfolioId]);
    // Bumped after every change, so the growth chart is fetched again together with the positions.
    const [version, setVersion] = useState(0);

    const [adding, setAdding] = useState(false);
    const [editing, setEditing] = useState<StockDto | null>(null);
    const [toDelete, setToDelete] = useState<StockDto | null>(null);
    const [deleting, setDeleting] = useState(false);
    const [refreshingPrices, setRefreshingPrices] = useState(false);
    const [pricesUpdatedAt, setUpdatedAt] = useState<Date | null>(() => getPricesUpdatedAt(portfolioId));

    usePageTitle(portfolio.data?.name);

    const positions = stocks.data ?? [];
    const summary = useMemo(() => summarise(positions), [positions]);
    const best = useMemo(
        () => positions.reduce<StockDto | null>((top, stock) => (!top || stock.gainPercentage > top.gainPercentage ? stock : top), null),
        [positions]
    );
    const sectorCount = useMemo(() => new Set(positions.map((stock) => stock.sector)).size, [positions]);
    const firstPurchase = useMemo(
        () => positions.reduce<number | null>((first, stock) => {
            const time = new Date(stock.entryDate).getTime();
            return first === null || time < first ? time : first;
        }, null),
        [positions]
    );

    const reloadAll = () => {
        portfolio.reload();
        stocks.reload();
        setVersion((value) => value + 1);
    };

    const handleRefreshPrices = async () => {
        setRefreshingPrices(true);
        try {
            await RefreshPrices(portfolioId);
            const now = new Date();
            setPricesUpdatedAt(portfolioId, now);
            setUpdatedAt(now);
            notify.success('Ціни оновлено');
            reloadAll();
        } catch (error) {
            notify.error(getErrorMessage(error, 'Не вдалося оновити ціни. Спробуйте ще раз трохи згодом.'));
        } finally {
            setRefreshingPrices(false);
        }
    };

    const handleDelete = async () => {
        if (!toDelete) return;
        setDeleting(true);
        try {
            await DeleteStock(toDelete.id);
            notify.success(`Позицію ${toDelete.symbol} видалено`);
            setToDelete(null);
            reloadAll();
        } catch (error) {
            notify.error(getErrorMessage(error, 'Не вдалося видалити позицію. Спробуйте ще раз.'));
        } finally {
            setDeleting(false);
        }
    };

    const notFound = axios.isAxiosError(portfolio.error) && portfolio.error.response?.status === 404;
    if (notFound) {
        return <PortfolioNotFound />;
    }

    const failure = portfolio.error ?? stocks.error;
    if (failure && !portfolio.data) {
        return (
            <Card>
                <ErrorState
                    title="Не вдалося відкрити портфель"
                    message={getErrorMessage(failure, 'Не вдалося завантажити портфель. Можливо, його видалено або адреса неправильна.')}
                    onRetry={() => {
                        portfolio.reload();
                        stocks.reload();
                    }}
                />
            </Card>
        );
    }

    if (!portfolio.data || stocks.loading) {
        return <DashboardSkeleton />;
    }

    const hasPositions = positions.length > 0;
    const name = portfolio.data.name;

    return (
        <>
            <PageHeader
                title={name}
                breadcrumbs={[{ label: 'Портфелі', to: '/' }, { label: name }]}
                subtitle={
                    <>
                        <AccessTime sx={{ fontSize: 16, color: 'text.disabled' }} />
                        {pricesUpdatedAt
                            ? `Ціни оновлено ${formatUpdatedAt(pricesUpdatedAt)}`
                            : 'Натисніть «Оновити ціни», щоб отримати актуальні котирування'}
                    </>
                }
                actions={
                    <>
                        <Button
                            variant="outlined"
                            onClick={handleRefreshPrices}
                            disabled={!hasPositions || refreshingPrices}
                            startIcon={refreshingPrices ? <CircularProgress size={16} color="inherit" /> : <Refresh />}
                        >
                            {refreshingPrices ? 'Оновлюємо…' : 'Оновити ціни'}
                        </Button>
                        <Button variant="outlined" startIcon={<Add />} onClick={() => setAdding(true)}>
                            Додати позицію
                        </Button>
                        <Button
                            variant="contained"
                            startIcon={<Tune />}
                            component={RouterLink}
                            to={`/portfolios/${portfolioId}/optimisation`}
                            disabled={!hasPositions}
                        >
                            Оптимізувати
                        </Button>
                    </>
                }
            />

            <KpiGrid>
                <KpiCard
                    label="Поточна вартість"
                    value={hasPositions ? formatMoney(summary.current) : '—'}
                    note={hasPositions ? `${positionsCount(positions.length)} · ${pluralize(sectorCount, { one: 'сектор', few: 'сектори', many: 'секторів' })}` : 'Немає позицій'}
                />
                <KpiCard
                    label="Вкладено"
                    value={hasPositions ? formatMoney(summary.invested) : '—'}
                    note={firstPurchase !== null ? `перша покупка ${formatDate(firstPurchase)}` : undefined}
                />
                <KpiCard
                    label="Прибуток / збиток"
                    value={hasPositions ? formatMoney(summary.gain, { signed: true }) : '—'}
                    delta={hasPositions ? <Delta format="percent" value={summary.gainPercentage} /> : undefined}
                    note={hasPositions ? 'за весь час' : undefined}
                />
                <KpiCard
                    label="Найкраща позиція"
                    value={best ? best.symbol : '—'}
                    delta={best ? <Delta format="percent" value={best.gainPercentage} /> : undefined}
                    note={best ? best.name || sectorName(best.sector) : undefined}
                />
            </KpiGrid>

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'minmax(0, 1fr)', lg: 'minmax(0, 2fr) minmax(0, 1fr)' }, gap: 2, alignItems: 'stretch' }}>
                <GrowthChart portfolioId={portfolioId} stocks={positions} version={version} />
                <SectorDonut stocks={positions} />
            </Box>

            <PositionsTable
                stocks={positions}
                onAdd={() => setAdding(true)}
                onChangeQuantity={setEditing}
                onDelete={setToDelete}
            />

            <AddPositionDialog
                open={adding}
                portfolioId={portfolioId}
                onClose={() => setAdding(false)}
                onAdded={(symbol) => {
                    setAdding(false);
                    notify.success(`Позицію ${symbol} додано`);
                    reloadAll();
                }}
            />

            <ChangeQuantityDialog
                stock={editing}
                onClose={() => setEditing(null)}
                onSaved={(stock, quantity) => {
                    setEditing(null);
                    notify.success(`Кількість ${stock.symbol} змінено на ${quantity}`);
                    reloadAll();
                }}
            />

            <ConfirmDialog
                open={toDelete !== null}
                title={`Видалити позицію ${toDelete?.symbol ?? ''}?`}
                message={`Позицію буде видалено з портфеля «${name}», а вартість і прибуток портфеля перераховано. Цю дію не можна скасувати.`}
                confirmLabel="Видалити"
                danger
                loading={deleting}
                onConfirm={handleDelete}
                onClose={() => setToDelete(null)}
            />
        </>
    );
}

export default PortfolioDashboard;
