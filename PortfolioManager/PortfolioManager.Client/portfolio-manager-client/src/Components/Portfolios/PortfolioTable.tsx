import { useState } from 'react';
import Box from '@mui/material/Box';
import CardActionArea from '@mui/material/CardActionArea';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import Link from '@mui/material/Link';
import ListItemIcon from '@mui/material/ListItemIcon';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';
import { visuallyHidden } from '@mui/utils';
import DeleteOutline from '@mui/icons-material/DeleteOutline';
import MoreVert from '@mui/icons-material/MoreVert';
import OpenInNew from '@mui/icons-material/OpenInNew';
import Tune from '@mui/icons-material/Tune';
import WorkOutline from '@mui/icons-material/WorkOutline';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import PortfolioDto from '../../DTO/PortfolioDto';
import { PortfolioSummary } from '../../Domain/summarise';
import { formatMoney, positionsCount } from '../../Format/format';
import Delta from '../Shared/Delta';

export interface PortfolioRow {
    portfolio: PortfolioDto;
    summary: PortfolioSummary;
    positions: number;
}

interface PortfolioTableProps {
    rows: PortfolioRow[];
    onDelete: (portfolio: PortfolioDto) => void;
}

const dashboardPath = (id: number) => `/portfolios/${id}`;
const optimisationPath = (id: number) => `/portfolios/${id}/optimisation`;

function RowMenu({ row, onDelete }: { row: PortfolioRow; onDelete: (portfolio: PortfolioDto) => void }) {
    const [anchor, setAnchor] = useState<HTMLElement | null>(null);
    const { portfolio, positions } = row;
    const close = () => setAnchor(null);
    const menuId = `portfolio-menu-${portfolio.id}`;

    return (
        <>
            <IconButton
                aria-label={`Дії з портфелем «${portfolio.name}»`}
                aria-haspopup="menu"
                aria-controls={anchor ? menuId : undefined}
                aria-expanded={anchor ? 'true' : undefined}
                onClick={(event) => {
                    event.stopPropagation();
                    setAnchor(event.currentTarget);
                }}
                sx={{ color: 'text.secondary' }}
            >
                <MoreVert fontSize="small" />
            </IconButton>
            <Menu
                id={menuId}
                anchorEl={anchor}
                open={Boolean(anchor)}
                onClose={close}
                onClick={(event) => event.stopPropagation()}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
            >
                <MenuItem component={RouterLink} to={dashboardPath(portfolio.id)} onClick={close}>
                    <ListItemIcon><OpenInNew fontSize="small" /></ListItemIcon>
                    Відкрити
                </MenuItem>
                <MenuItem component={RouterLink} to={optimisationPath(portfolio.id)} onClick={close} disabled={positions === 0}>
                    <ListItemIcon><Tune fontSize="small" /></ListItemIcon>
                    Оптимізувати
                </MenuItem>
                <Divider sx={{ my: 0.5 }} />
                <MenuItem
                    onClick={() => {
                        close();
                        onDelete(portfolio);
                    }}
                    sx={{ color: 'error.main' }}
                >
                    <ListItemIcon sx={{ color: 'inherit' }}><DeleteOutline fontSize="small" /></ListItemIcon>
                    Видалити…
                </MenuItem>
            </Menu>
        </>
    );
}

function TableView({ rows, onDelete }: PortfolioTableProps) {
    const navigate = useNavigate();

    return (
        <TableContainer>
            <Table aria-label="Портфелі" sx={{ minWidth: 760 }}>
                <TableHead>
                    <TableRow>
                        <TableCell>Назва</TableCell>
                        <TableCell align="right">Вкладено</TableCell>
                        <TableCell align="right">Поточна вартість</TableCell>
                        <TableCell align="right">Прибуток / збиток</TableCell>
                        <TableCell align="right">Дохідність</TableCell>
                        <TableCell padding="checkbox">
                            <Box component="span" sx={visuallyHidden}>Дії</Box>
                        </TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {rows.map((row) => {
                        const { portfolio, summary, positions } = row;
                        const filled = positions > 0;
                        return (
                            <TableRow
                                key={portfolio.id}
                                hover
                                onClick={() => navigate(dashboardPath(portfolio.id))}
                                sx={{ cursor: 'pointer' }}
                            >
                                <TableCell>
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, minWidth: 0 }}>
                                        <Box
                                            aria-hidden
                                            sx={{ width: 36, height: 36, flex: 'none', borderRadius: 1, background: (t) => t.app.surface2, color: 'text.secondary', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
                                        >
                                            <WorkOutline fontSize="small" />
                                        </Box>
                                        <Box sx={{ minWidth: 0 }}>
                                            <Link
                                                component={RouterLink}
                                                to={dashboardPath(portfolio.id)}
                                                onClick={(event) => event.stopPropagation()}
                                                sx={{ display: 'block', color: 'text.primary', fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 320 }}
                                            >
                                                {portfolio.name}
                                            </Link>
                                            <Typography variant="caption" color="text.disabled">
                                                {filled ? positionsCount(positions) : 'Немає позицій'}
                                            </Typography>
                                        </Box>
                                    </Box>
                                </TableCell>
                                <TableCell align="right" sx={{ color: filled ? 'text.secondary' : 'text.disabled' }}>
                                    {filled ? formatMoney(summary.invested) : '—'}
                                </TableCell>
                                <TableCell align="right" sx={{ fontWeight: 600, color: filled ? 'text.primary' : 'text.disabled' }}>
                                    {filled ? formatMoney(summary.current) : '—'}
                                </TableCell>
                                <TableCell align="right">
                                    {filled ? <Delta variant="text" format="money" value={summary.gain} /> : <Typography component="span" color="text.disabled">—</Typography>}
                                </TableCell>
                                <TableCell align="right">
                                    {filled ? <Delta format="percent" value={summary.gainPercentage} /> : <Typography component="span" color="text.disabled">—</Typography>}
                                </TableCell>
                                <TableCell padding="checkbox" onClick={(event) => event.stopPropagation()}>
                                    <RowMenu row={row} onDelete={onDelete} />
                                </TableCell>
                            </TableRow>
                        );
                    })}
                </TableBody>
            </Table>
        </TableContainer>
    );
}

function CardsView({ rows, onDelete }: PortfolioTableProps) {
    return (
        <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0, display: 'flex', flexDirection: 'column' }}>
            {rows.map((row, index) => {
                const { portfolio, summary, positions } = row;
                const filled = positions > 0;
                return (
                    <Box
                        component="li"
                        key={portfolio.id}
                        sx={{ position: 'relative', borderTop: index === 0 ? 0 : 1, borderColor: 'divider' }}
                    >
                        <CardActionArea component={RouterLink} to={dashboardPath(portfolio.id)} sx={{ p: 2, pr: 7, display: 'block' }}>
                            <Typography fontWeight={600} sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                {portfolio.name}
                            </Typography>
                            <Typography variant="caption" color="text.disabled">
                                {filled ? positionsCount(positions) : 'Немає позицій'}
                            </Typography>
                            <Box sx={{ mt: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}>
                                <Typography variant="h2" component="span" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                    {filled ? formatMoney(summary.current) : '—'}
                                </Typography>
                                {filled && <Delta format="percent" value={summary.gainPercentage} />}
                            </Box>
                            {filled && (
                                <Box sx={{ mt: 0.5, display: 'flex', justifyContent: 'space-between', fontSize: '0.8125rem', color: 'text.secondary' }}>
                                    <span>Вкладено {formatMoney(summary.invested)}</span>
                                    <Delta variant="text" format="money" value={summary.gain} />
                                </Box>
                            )}
                        </CardActionArea>
                        <Box sx={{ position: 'absolute', top: 8, right: 8 }}>
                            <RowMenu row={row} onDelete={onDelete} />
                        </Box>
                    </Box>
                );
            })}
        </Box>
    );
}

/** The portfolios as a table, or as a list of cards on a phone. */
export default function PortfolioTable(props: PortfolioTableProps) {
    const theme = useTheme();
    const compact = useMediaQuery(theme.breakpoints.down('md'));
    return compact ? <CardsView {...props} /> : <TableView {...props} />;
}
