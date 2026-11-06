import { ReactNode, useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TableSortLabel from '@mui/material/TableSortLabel';
import Typography from '@mui/material/Typography';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';
import { visuallyHidden } from '@mui/utils';
import Add from '@mui/icons-material/Add';
import DeleteOutline from '@mui/icons-material/DeleteOutline';
import EditOutlined from '@mui/icons-material/EditOutlined';
import ListAlt from '@mui/icons-material/ListAlt';
import MoreVert from '@mui/icons-material/MoreVert';
import StockDto from '../../DTO/StockDto';
import { sectorColor, sectorName } from '../../Domain/sectors';
import { formatDate, formatMoney, formatNumber } from '../../Format/format';
import { useChartColors } from '../../Theme/useChartColors';
import Delta from '../Shared/Delta';
import EmptyState from '../Shared/EmptyState';

type SortKey = 'symbol' | 'sector' | 'quantity' | 'entryPrice' | 'currentPrice' | 'value' | 'gain' | 'gainPercentage' | 'entryDate';
type Direction = 'asc' | 'desc';

interface Column {
    key: SortKey;
    label: string;
    numeric?: boolean;
}

const COLUMNS: Column[] = [
    { key: 'symbol', label: 'Позиція' },
    { key: 'sector', label: 'Сектор' },
    { key: 'quantity', label: 'Кількість', numeric: true },
    { key: 'entryPrice', label: 'Ціна входу', numeric: true },
    { key: 'currentPrice', label: 'Поточна ціна', numeric: true },
    { key: 'value', label: 'Вартість', numeric: true },
    { key: 'gain', label: 'Прибуток', numeric: true },
    { key: 'gainPercentage', label: 'Дохідність', numeric: true },
    { key: 'entryDate', label: 'Дата входу', numeric: true },
];

/** On a phone or a small tablet only these fit: the sector moves under the ticker and the return under the value. */
const COMPACT_KEYS: SortKey[] = ['symbol', 'value'];

function sortValue(stock: StockDto, key: SortKey): string | number {
    switch (key) {
        case 'symbol': return stock.symbol;
        case 'sector': return sectorName(stock.sector);
        case 'quantity': return stock.quantity;
        case 'entryPrice': return stock.entryPrice;
        case 'currentPrice': return stock.currentPrice;
        case 'value': return stock.totalCurrentPrice;
        case 'gain': return stock.gain;
        case 'gainPercentage': return stock.gainPercentage;
        case 'entryDate': return new Date(stock.entryDate).getTime();
    }
}

function sortStocks(stocks: StockDto[], key: SortKey, direction: Direction): StockDto[] {
    const sign = direction === 'asc' ? 1 : -1;
    return [...stocks].sort((a, b) => {
        const left = sortValue(a, key);
        const right = sortValue(b, key);
        const order = typeof left === 'string' ? left.localeCompare(String(right), 'uk') : (left as number) - (right as number);
        return order * sign || a.symbol.localeCompare(b.symbol);
    });
}

function SectorChip({ sector }: { sector: number }) {
    const colors = useChartColors();
    return (
        <Box
            component="span"
            sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75, maxWidth: '100%', height: 24, px: 1, borderRadius: '6px', background: (t) => t.app.surface2, color: 'text.secondary', fontSize: '0.75rem', fontWeight: 500 }}
        >
            <Box aria-hidden component="span" sx={{ width: 8, height: 8, flex: 'none', borderRadius: '2px', background: sectorColor(sector, colors.categorical, colors.otherCategory) }} />
            <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{sectorName(sector)}</Box>
        </Box>
    );
}

function RowMenu({ stock, onChangeQuantity, onDelete }: { stock: StockDto; onChangeQuantity: (stock: StockDto) => void; onDelete: (stock: StockDto) => void }) {
    const [anchor, setAnchor] = useState<HTMLElement | null>(null);
    const close = () => setAnchor(null);
    const menuId = `position-menu-${stock.id}`;

    return (
        <>
            <IconButton
                aria-label={`Дії з позицією ${stock.symbol}`}
                aria-haspopup="menu"
                aria-controls={anchor ? menuId : undefined}
                aria-expanded={anchor ? 'true' : undefined}
                onClick={(event) => setAnchor(event.currentTarget)}
                sx={{ color: 'text.secondary' }}
            >
                <MoreVert fontSize="small" />
            </IconButton>
            <Menu
                id={menuId}
                anchorEl={anchor}
                open={Boolean(anchor)}
                onClose={close}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
            >
                <MenuItem onClick={() => { close(); onChangeQuantity(stock); }}>
                    <ListItemIcon><EditOutlined fontSize="small" /></ListItemIcon>
                    Змінити кількість
                </MenuItem>
                <Divider sx={{ my: 0.5 }} />
                <MenuItem onClick={() => { close(); onDelete(stock); }} sx={{ color: 'error.main' }}>
                    <ListItemIcon sx={{ color: 'inherit' }}><DeleteOutline fontSize="small" /></ListItemIcon>
                    Видалити…
                </MenuItem>
            </Menu>
        </>
    );
}

/** The cell of one column for one position. */
function renderCell(stock: StockDto, key: SortKey, compact: boolean): ReactNode {
    const currency = stock.currency || 'USD';
    switch (key) {
        case 'symbol':
            return (
                <TableCell key={key} sx={{ position: 'sticky', left: 0, zIndex: 1, background: 'inherit', pl: { xs: 2, md: 2.5 }, maxWidth: compact ? 170 : undefined }}>
                    <Typography fontWeight={600}>{stock.symbol}</Typography>
                    <Typography variant="caption" color="text.disabled" sx={{ display: 'block', maxWidth: compact ? 140 : 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {stock.name}
                    </Typography>
                    {compact && <Box sx={{ mt: 0.5 }}><SectorChip sector={stock.sector} /></Box>}
                </TableCell>
            );
        case 'sector':
            return <TableCell key={key} sx={{ maxWidth: 200 }}><SectorChip sector={stock.sector} /></TableCell>;
        case 'quantity':
            return <TableCell key={key} align="right">{formatNumber(stock.quantity)}</TableCell>;
        case 'entryPrice':
            return <TableCell key={key} align="right" sx={{ color: 'text.secondary' }}>{formatMoney(stock.entryPrice, { currency })}</TableCell>;
        case 'currentPrice':
            return <TableCell key={key} align="right">{formatMoney(stock.currentPrice, { currency })}</TableCell>;
        case 'value':
            return compact ? (
                <TableCell key={key} align="right">
                    <Typography fontWeight={600} sx={{ fontVariantNumeric: 'tabular-nums' }}>{formatMoney(stock.totalCurrentPrice, { currency })}</Typography>
                    <Box sx={{ mt: 0.5 }}><Delta format="percent" value={stock.gainPercentage} /></Box>
                </TableCell>
            ) : (
                <TableCell key={key} align="right" sx={{ fontWeight: 600 }}>{formatMoney(stock.totalCurrentPrice, { currency })}</TableCell>
            );
        case 'gain':
            return <TableCell key={key} align="right"><Delta variant="text" format="money" currency={currency} value={stock.gain} /></TableCell>;
        case 'gainPercentage':
            return <TableCell key={key} align="right"><Delta format="percent" value={stock.gainPercentage} /></TableCell>;
        case 'entryDate':
            return <TableCell key={key} align="right" sx={{ color: 'text.secondary' }}>{formatDate(stock.entryDate)}</TableCell>;
    }
}

interface PositionsTableProps {
    stocks: StockDto[];
    onAdd: () => void;
    onChangeQuantity: (stock: StockDto) => void;
    onDelete: (stock: StockDto) => void;
}

export default function PositionsTable({ stocks, onAdd, onChangeQuantity, onDelete }: PositionsTableProps) {
    const theme = useTheme();
    const compact = useMediaQuery(theme.breakpoints.down('md'));
    const [sort, setSort] = useState<{ key: SortKey; direction: Direction }>({ key: 'value', direction: 'desc' });
    const rows = useMemo(() => sortStocks(stocks, sort.key, sort.direction), [stocks, sort]);
    const columns = compact ? COLUMNS.filter((column) => COMPACT_KEYS.includes(column.key)) : COLUMNS;

    const handleSort = (key: SortKey) =>
        setSort((current) =>
            current.key === key
                ? { key, direction: current.direction === 'asc' ? 'desc' : 'asc' }
                : // Text sorts A to Z first, figures biggest first.
                  { key, direction: key === 'symbol' || key === 'sector' ? 'asc' : 'desc' }
        );

    return (
        <Card component="section" aria-labelledby="positions-title" sx={{ minWidth: 0 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, px: { xs: 2, md: 2.5 }, py: 2 }}>
                <Typography id="positions-title" variant="h2">Позиції</Typography>
                <Box component="span" sx={{ height: 22, px: 1, borderRadius: 999, background: (t) => t.app.surface2, color: 'text.secondary', fontSize: '0.75rem', fontWeight: 600, display: 'inline-flex', alignItems: 'center' }}>
                    {formatNumber(stocks.length)}
                </Box>
            </Box>

            {stocks.length === 0 ? (
                <EmptyState
                    icon={<ListAlt />}
                    title="У портфелі ще немає позицій"
                    description="Додайте першу акцію: вкажіть тікер, кількість і ціну, за якою ви її купили."
                    action={<Button variant="contained" startIcon={<Add />} onClick={onAdd}>Додати позицію</Button>}
                />
            ) : (
                <TableContainer>
                    <Table
                        aria-label="Позиції портфеля"
                        // Ten columns have to fit a 1200px page; cells in the middle are a little tighter than usual.
                        sx={{
                            minWidth: compact ? 0 : 980,
                            '& .MuiTableCell-root:not(:first-of-type):not(:last-of-type)': { px: 1 },
                            '& .MuiTableCell-root:last-of-type': { px: 0.5, width: 52 },
                        }}
                    >
                        <TableHead>
                            <TableRow>
                                {columns.map((column, index) => (
                                    <TableCell
                                        key={column.key}
                                        align={column.numeric ? 'right' : 'left'}
                                        sortDirection={sort.key === column.key ? sort.direction : false}
                                        sx={index === 0 ? { position: 'sticky', left: 0, zIndex: 1, pl: { xs: 2, md: 2.5 } } : undefined}
                                    >
                                        <TableSortLabel
                                            active={sort.key === column.key}
                                            direction={sort.key === column.key ? sort.direction : 'asc'}
                                            onClick={() => handleSort(column.key)}
                                        >
                                            {column.label}
                                            {sort.key === column.key && (
                                                <Box component="span" sx={visuallyHidden}>
                                                    {sort.direction === 'desc' ? ': за спаданням' : ': за зростанням'}
                                                </Box>
                                            )}
                                        </TableSortLabel>
                                    </TableCell>
                                ))}
                                <TableCell padding="checkbox">
                                    <Box component="span" sx={visuallyHidden}>Дії</Box>
                                </TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {rows.map((stock) => (
                                <TableRow key={stock.id} hover sx={{ background: (t) => t.app.surface, '&:hover': { background: (t) => t.app.hover } }}>
                                    {columns.map((column) => renderCell(stock, column.key, compact))}
                                    <TableCell padding="checkbox">
                                        <RowMenu stock={stock} onChangeQuantity={onChangeQuantity} onDelete={onDelete} />
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            )}
        </Card>
    );
}
