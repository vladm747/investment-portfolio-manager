import { useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Typography from '@mui/material/Typography';
import Add from '@mui/icons-material/Add';
import WorkOutline from '@mui/icons-material/WorkOutline';
import PortfolioDto from '../../DTO/PortfolioDto';
import { summarise } from '../../Domain/summarise';
import { formatMoney, pluralize, positionsCount } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { usePageTitle } from '../../Hooks/usePageTitle';
import { CreatePortfolio, DeletePortfolio, GetAllPortfoliosAsync } from '../../Services/PortfolioService';
import { getErrorMessage } from '../../Services/errors';
import ConfirmDialog from '../Shared/ConfirmDialog';
import Delta from '../Shared/Delta';
import EmptyState from '../Shared/EmptyState';
import ErrorState from '../Shared/ErrorState';
import KpiCard, { KpiGrid } from '../Shared/KpiCard';
import { useNotify } from '../Shared/Notifications';
import PageHeader from '../Shared/PageHeader';
import { KpiRowSkeleton, TableSkeleton } from '../Shared/Skeletons';
import CreatePortfolioDialog from './CreatePortfolioDialog';
import PortfolioTable, { PortfolioRow } from './PortfolioTable';

const portfoliosCount = (count: number) =>
    pluralize(count, { one: 'портфель', few: 'портфелі', many: 'портфелів' });

export default function PortfoliosPage() {
    usePageTitle('Портфелі');
    const notify = useNotify();
    const portfolios = useAsyncData(GetAllPortfoliosAsync, []);

    const [creating, setCreating] = useState(false);
    const [toDelete, setToDelete] = useState<PortfolioDto | null>(null);
    const [deleting, setDeleting] = useState(false);

    const rows = useMemo<PortfolioRow[]>(
        () =>
            (portfolios.data ?? []).map((portfolio) => ({
                portfolio,
                summary: summarise(portfolio.stocks ?? []),
                positions: portfolio.stocks?.length ?? 0,
            })),
        [portfolios.data]
    );

    const totals = useMemo(() => {
        const filled = rows.filter((row) => row.positions > 0);
        const invested = filled.reduce((sum, row) => sum + row.summary.invested, 0);
        const current = filled.reduce((sum, row) => sum + row.summary.current, 0);
        const gain = current - invested;
        const best = filled
            .filter((row) => row.summary.invested > 0)
            .reduce<PortfolioRow | null>((top, row) => (!top || row.summary.gainPercentage > top.summary.gainPercentage ? row : top), null);
        return {
            invested,
            current,
            gain,
            gainPercentage: invested > 0 ? (gain / invested) * 100 : 0,
            positions: filled.reduce((sum, row) => sum + row.positions, 0),
            best,
        };
    }, [rows]);

    const handleCreate = async (name: string) => {
        await CreatePortfolio({ name });
        setCreating(false);
        notify.success(`Портфель «${name}» створено`);
        portfolios.reload();
    };

    const handleDelete = async () => {
        if (!toDelete) return;
        setDeleting(true);
        try {
            await DeletePortfolio(toDelete.id);
            notify.success(`Портфель «${toDelete.name}» видалено`);
            setToDelete(null);
            portfolios.reload();
        } catch (error) {
            notify.error(getErrorMessage(error, 'Не вдалося видалити портфель. Спробуйте ще раз.'));
        } finally {
            setDeleting(false);
        }
    };

    const loaded = portfolios.data !== undefined;
    const hasRows = rows.length > 0;
    const hasValues = totals.positions > 0;

    return (
        <>
            <PageHeader
                title="Портфелі"
                subtitle={
                    loaded && hasRows
                        ? `${portfoliosCount(rows.length)} · ${positionsCount(totals.positions)}`
                        : undefined
                }
                actions={
                    <Button variant="contained" startIcon={<Add />} onClick={() => setCreating(true)}>
                        Створити портфель
                    </Button>
                }
            />

            {portfolios.loading && (
                <>
                    <KpiRowSkeleton />
                    <Card>
                        <TableSkeleton rows={4} columns={5} />
                    </Card>
                </>
            )}

            {!portfolios.loading && !loaded && (
                <Card>
                    <ErrorState
                        message={getErrorMessage(portfolios.error, 'Не вдалося завантажити портфелі.')}
                        onRetry={portfolios.reload}
                    />
                </Card>
            )}

            {loaded && !hasRows && (
                <Card>
                    <EmptyState
                        icon={<WorkOutline />}
                        title="Ще немає портфелів"
                        description="Створіть перший портфель, щоб додати до нього акції та побачити, як змінюється його вартість."
                        action={
                            <Button variant="contained" startIcon={<Add />} onClick={() => setCreating(true)}>
                                Створити портфель
                            </Button>
                        }
                    />
                </Card>
            )}

            {loaded && hasRows && (
                <>
                    <KpiGrid>
                        <KpiCard
                            label="Загальна вартість"
                            value={hasValues ? formatMoney(totals.current) : '—'}
                            note={`${portfoliosCount(rows.length)} · ${positionsCount(totals.positions)}`}
                        />
                        <KpiCard
                            label="Вкладено"
                            value={hasValues ? formatMoney(totals.invested) : '—'}
                            note="за цінами входу"
                        />
                        <KpiCard
                            label="Прибуток / збиток"
                            value={hasValues ? formatMoney(totals.gain, { signed: true }) : '—'}
                            delta={hasValues ? <Delta format="percent" value={totals.gainPercentage} /> : undefined}
                            note={hasValues ? 'за весь час' : undefined}
                        />
                        <KpiCard
                            label="Найкращий портфель"
                            textValue
                            value={totals.best ? totals.best.portfolio.name : '—'}
                            delta={totals.best ? <Delta format="percent" value={totals.best.summary.gainPercentage} /> : undefined}
                            note={totals.best ? positionsCount(totals.best.positions) : undefined}
                        />
                    </KpiGrid>

                    <Card component="section" aria-labelledby="all-portfolios">
                        <Box sx={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 2, px: 2.5, py: 2 }}>
                            <Typography id="all-portfolios" variant="h2">Усі портфелі</Typography>
                            <Typography variant="caption" color="text.disabled" sx={{ display: { xs: 'none', md: 'inline' } }}>
                                Натисніть на рядок, щоб відкрити портфель
                            </Typography>
                        </Box>
                        <PortfolioTable rows={rows} onDelete={setToDelete} />
                    </Card>
                </>
            )}

            <CreatePortfolioDialog open={creating} onClose={() => setCreating(false)} onCreate={handleCreate} />

            <ConfirmDialog
                open={toDelete !== null}
                title={`Видалити портфель «${toDelete?.name ?? ''}»?`}
                message={
                    <>
                        Портфель
                        {toDelete?.stocks?.length ? ` і всі його позиції (${toDelete.stocks.length})` : ''} буде видалено
                        назавжди. Цю дію не можна скасувати.
                    </>
                }
                confirmLabel="Видалити"
                danger
                loading={deleting}
                onConfirm={handleDelete}
                onClose={() => setToDelete(null)}
            />
        </>
    );
}
