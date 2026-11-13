import { useMemo, useState } from 'react';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import InputAdornment from '@mui/material/InputAdornment';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import PeopleOutline from '@mui/icons-material/PeopleOutline';
import Search from '@mui/icons-material/Search';
import SearchOff from '@mui/icons-material/SearchOff';
import { AdminUserDto } from '../../DTO/AdminUsersDto';
import { ADMIN_ROLE } from '../../Auth/roles';
import { formatDate, formatNumber, formatUpdatedAt, pluralize } from '../../Format/format';
import { useAsyncData } from '../../Hooks/useAsyncData';
import { usePageTitle } from '../../Hooks/usePageTitle';
import { GetAdminUsersAsync } from '../../Services/AdminService';
import { getErrorMessage } from '../../Services/errors';
import EmptyState from '../Shared/EmptyState';
import ErrorState from '../Shared/ErrorState';
import KpiCard, { KpiGrid } from '../Shared/KpiCard';
import PageHeader from '../Shared/PageHeader';
import { KpiRowSkeleton, TableSkeleton } from '../Shared/Skeletons';
import { initialsOf } from '../Layout/TopBar';

const ROLE_LABELS: Record<string, string> = {
    admin: 'Адміністратор',
    investor: 'Інвестор',
};

const usersCount = (count: number) =>
    pluralize(count, { one: 'користувач', few: 'користувачі', many: 'користувачів' });

const matches = (user: AdminUserDto, query: string) =>
    user.fullName.toLocaleLowerCase('uk-UA').includes(query) || user.email.toLocaleLowerCase('uk-UA').includes(query);

function RoleChips({ roles }: { roles: string[] }) {
    if (roles.length === 0) {
        return <Typography component="span" color="text.disabled">—</Typography>;
    }
    return (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
            {roles.map((role) => (
                <Chip
                    key={role}
                    size="small"
                    label={ROLE_LABELS[role] ?? role}
                    sx={(theme) => ({
                        fontWeight: 500,
                        background: role === ADMIN_ROLE ? theme.app.accentSoft : theme.app.surface2,
                        color: role === ADMIN_ROLE ? theme.app.accentText : theme.palette.text.secondary,
                    })}
                />
            ))}
        </Box>
    );
}

function UsersTable({ users }: { users: AdminUserDto[] }) {
    return (
        <TableContainer>
            <Table aria-label="Користувачі" sx={{ minWidth: 680 }}>
                <TableHead>
                    <TableRow>
                        <TableCell>Користувач</TableCell>
                        <TableCell>Пошта</TableCell>
                        <TableCell>Ролі</TableCell>
                        <TableCell align="right">Портфелі</TableCell>
                        <TableCell align="right">Остання активність</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {users.map((user) => (
                        <TableRow key={user.id} hover>
                            <TableCell>
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, minWidth: 0 }}>
                                    <Avatar
                                        aria-hidden
                                        sx={(theme) => ({ width: 32, height: 32, flex: 'none', fontSize: 12, fontWeight: 600, background: theme.app.accentSoft, color: theme.app.accentText })}
                                    >
                                        {initialsOf(user.fullName, user.email)}
                                    </Avatar>
                                    <Typography fontWeight={600} sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 200 }}>
                                        {user.fullName || user.email}
                                    </Typography>
                                </Box>
                            </TableCell>
                            <TableCell sx={{ color: 'text.secondary' }}>{user.email}</TableCell>
                            <TableCell>
                                <RoleChips roles={user.roles} />
                            </TableCell>
                            <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                                {formatNumber(user.portfolioCount)}
                            </TableCell>
                            <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                                {user.lastActiveAt ? (
                                    <Box component="span" title={formatUpdatedAt(user.lastActiveAt)}>
                                        {formatDate(user.lastActiveAt)}
                                    </Box>
                                ) : (
                                    <Typography component="span" color="text.disabled">Ніколи</Typography>
                                )}
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    );
}

/** The admin dashboard: four totals and every user's profile and counts. Read-only; never anybody's portfolio contents. */
export default function UsersPage() {
    usePageTitle('Користувачі');
    const result = useAsyncData(GetAdminUsersAsync, []);
    const [query, setQuery] = useState('');

    const all = result.data?.users;
    const shown = useMemo(() => {
        const text = query.trim().toLocaleLowerCase('uk-UA');
        return text ? (all ?? []).filter((user) => matches(user, text)) : (all ?? []);
    }, [all, query]);

    const loaded = result.data !== undefined;
    const stats = result.data?.stats;
    const searching = query.trim() !== '';

    return (
        <>
            <PageHeader
                title="Користувачі"
                subtitle={all ? usersCount(all.length) : undefined}
            />

            {result.loading && (
                <>
                    <KpiRowSkeleton />
                    <Card>
                        <TableSkeleton rows={5} columns={5} />
                    </Card>
                </>
            )}

            {!result.loading && !loaded && (
                <Card>
                    <ErrorState
                        message={getErrorMessage(result.error, 'Не вдалося завантажити користувачів.')}
                        onRetry={result.reload}
                    />
                </Card>
            )}

            {loaded && stats && (
                <>
                    <KpiGrid>
                        <KpiCard label="Усього користувачів" value={formatNumber(stats.totalUsers)} note="акаунтів Google" />
                        <KpiCard label="Адміністратори" value={formatNumber(stats.admins)} note="за конфігурацією" />
                        <KpiCard label="Активні за 7 днів" value={formatNumber(stats.activeLast7Days)} note="входили в систему" />
                        <KpiCard label="Усього портфелів" value={formatNumber(stats.totalPortfolios)} note="у всіх користувачів" />
                    </KpiGrid>

                    <Card component="section" aria-labelledby="all-users">
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 2, px: 2.5, py: 2 }}>
                            <Typography id="all-users" variant="h2">Усі користувачі</Typography>
                            <TextField
                                type="search"
                                size="small"
                                value={query}
                                onChange={(event) => setQuery(event.target.value)}
                                placeholder="Пошук за ім’ям або поштою"
                                inputProps={{ 'aria-label': 'Пошук за ім’ям або поштою' }}
                                InputProps={{
                                    startAdornment: (
                                        <InputAdornment position="start">
                                            <Search fontSize="small" />
                                        </InputAdornment>
                                    ),
                                }}
                                sx={{ width: { xs: '100%', sm: 320 } }}
                            />
                        </Box>

                        {shown.length > 0 && <UsersTable users={shown} />}

                        {shown.length === 0 && searching && (
                            <EmptyState
                                icon={<SearchOff />}
                                title="Нікого не знайдено"
                                description="Пошук іде за ім’ям і поштою. Змініть запит або скиньте його."
                                action={<Button variant="outlined" onClick={() => setQuery('')}>Скинути пошук</Button>}
                            />
                        )}

                        {shown.length === 0 && !searching && (
                            <EmptyState
                                icon={<PeopleOutline />}
                                title="Користувачів ще немає"
                                description="Тут з’являться акаунти після першого входу через Google."
                            />
                        )}
                    </Card>
                </>
            )}
        </>
    );
}
