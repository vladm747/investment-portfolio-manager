import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import UsersPage from './UsersPage';
import AdminUsersDto, { AdminUserDto } from '../../DTO/AdminUsersDto';
import { fakeApi } from '../../testing/fakeApi';
import { renderWithProviders } from '../../testing/renderWithProviders';

const plain = (text: string | null) => (text ?? '').replace(/[  ]/g, ' ');

const user = (overrides: Partial<AdminUserDto>): AdminUserDto => ({
    id: 'u-' + (overrides.email ?? 'x'),
    fullName: 'Користувач',
    email: 'user@example.com',
    roles: ['investor'],
    portfolioCount: 0,
    lastActiveAt: '2026-11-10T09:30:00Z',
    ...overrides,
});

const olena = user({ fullName: 'Олена Коваль', email: 'olena@example.com', portfolioCount: 2 });
const admin = user({ fullName: 'Адмін Тестовий', email: 'admin@example.com', roles: ['admin', 'investor'], portfolioCount: 1 });
const never = user({ fullName: 'Іван Мельник', email: 'ivan@gmail.com', lastActiveAt: null });

const dashboard = (users: AdminUserDto[]): AdminUsersDto => ({
    stats: { totalUsers: users.length, admins: 1, activeLast7Days: 2, totalPortfolios: 3 },
    users,
});

const showUsers = (users: AdminUserDto[]) => fakeApi(() => ({ status: 200, data: dashboard(users) }));

const renderPage = () => renderWithProviders(<UsersPage />);

const card = (label: string) => screen.getByRole('heading', { name: label }).closest('section')!;

test('shows the four totals', async () => {
    showUsers([olena, admin, never]);

    renderPage();

    expect(await screen.findByText('olena@example.com')).toBeInTheDocument();
    expect(within(card('Усього користувачів')).getByText('3')).toBeInTheDocument();
    expect(within(card('Адміністратори')).getByText('1')).toBeInTheDocument();
    expect(within(card('Активні за 7 днів')).getByText('2')).toBeInTheDocument();
    expect(within(card('Усього портфелів')).getByText('3')).toBeInTheDocument();
    expect(document.title).toBe('Користувачі · Portfolio Manager');
});

test('lists one row per user with name, email, roles, portfolios and last activity', async () => {
    const requests = showUsers([olena, admin, never]);

    renderPage();

    await screen.findByText('olena@example.com');
    expect(requests.map((request) => request.url)).toEqual(['admin/users']);
    const rows = screen.getAllByRole('row');
    expect(rows).toHaveLength(4); // header and three users

    const olenaRow = within(rows[1]);
    expect(olenaRow.getByText('Олена Коваль')).toBeInTheDocument();
    expect(olenaRow.getByText('Інвестор')).toBeInTheDocument();
    expect(plain(rows[1].textContent)).toContain('10.11.2026');

    const adminRow = within(rows[2]);
    expect(adminRow.getByText('Адміністратор')).toBeInTheDocument();
    expect(adminRow.getByText('Інвестор')).toBeInTheDocument();
});

test('a user who never signed in says so', async () => {
    showUsers([never]);

    renderPage();

    await screen.findByText('ivan@gmail.com');
    expect(within(screen.getAllByRole('row')[1]).getByText('Ніколи')).toBeInTheDocument();
});

test('the page offers no way to change anybody: it is read-only', async () => {
    showUsers([olena, admin]);

    renderPage();

    await screen.findByText('olena@example.com');
    const tableButtons = within(screen.getByRole('table', { name: 'Користувачі' })).queryAllByRole('button');
    expect(tableButtons).toHaveLength(0);
    expect(screen.queryByRole('button', { name: /Видалити|Заблокувати|Змінити/ })).not.toBeInTheDocument();
});

test('search narrows the list by name or by email, ignoring case', async () => {
    showUsers([olena, admin, never]);
    renderPage();
    await screen.findByText('olena@example.com');
    const search = screen.getByRole('searchbox', { name: 'Пошук за ім’ям або поштою' });

    await userEvent.type(search, 'КОВАЛЬ');
    expect(screen.getAllByRole('row')).toHaveLength(2);
    expect(screen.getByText('olena@example.com')).toBeInTheDocument();

    await userEvent.clear(search);
    await userEvent.type(search, 'gmail');
    expect(screen.getAllByRole('row')).toHaveLength(2);
    expect(screen.getByText('ivan@gmail.com')).toBeInTheDocument();
    expect(screen.queryByText('olena@example.com')).not.toBeInTheDocument();
});

test('a search with no match says so and can be cleared', async () => {
    showUsers([olena, admin]);
    renderPage();
    await screen.findByText('olena@example.com');

    await userEvent.type(screen.getByRole('searchbox'), 'нікого-такого');

    expect(screen.getByText('Нікого не знайдено')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Скинути пошук' }));
    expect(screen.getByText('olena@example.com')).toBeInTheDocument();
});

test('shows an empty state when nobody has signed in yet', async () => {
    showUsers([]);

    renderPage();

    expect(await screen.findByText('Користувачів ще немає')).toBeInTheDocument();
});

test('shows placeholders while loading', () => {
    fakeApi(() => ({ status: 200, data: dashboard([olena]) }));

    renderPage();

    expect(screen.getByRole('table', { name: 'Завантаження' })).toBeInTheDocument();
});

test('a failed load shows an error and a retry button that loads again', async () => {
    let calls = 0;
    fakeApi(() => (++calls === 1 ? { status: 500 } : { status: 200, data: dashboard([olena]) }));
    renderPage();

    expect(await screen.findByText('Не вдалося завантажити користувачів.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Спробувати ще раз' }));

    expect(await screen.findByText('olena@example.com')).toBeInTheDocument();
    expect(calls).toBe(2);
});

test('a refusal (403) is shown as an error, not as an empty list', async () => {
    fakeApi(() => ({ status: 403 }));

    renderPage();

    expect(await screen.findByRole('alert')).toHaveTextContent('Не вдалося завантажити користувачів.');
    expect(screen.queryByText('Користувачів ще немає')).not.toBeInTheDocument();
});
