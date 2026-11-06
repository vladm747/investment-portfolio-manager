import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeModeProvider, THEME_MODE_KEY } from './ThemeModeProvider';
import ThemeToggle from './ThemeToggle';
import { setSystemColorScheme } from '../testing/renderWithProviders';

const renderToggle = () =>
    render(
        <ThemeModeProvider>
            <ThemeToggle />
        </ThemeModeProvider>
    );

const currentMode = () => document.documentElement.dataset.theme;

beforeEach(() => {
    window.localStorage.clear();
    delete document.documentElement.dataset.theme;
});

afterEach(() => {
    vi.restoreAllMocks();
});

test('without a saved choice the theme follows the system: light', () => {
    setSystemColorScheme(false);
    renderToggle();

    expect(currentMode()).toBe('light');
    expect(screen.getByRole('button', { name: 'Увімкнути темну тему' })).toBeInTheDocument();
});

test('without a saved choice the theme follows the system: dark', () => {
    setSystemColorScheme(true);
    renderToggle();

    expect(currentMode()).toBe('dark');
    expect(screen.getByRole('button', { name: 'Увімкнути світлу тему' })).toBeInTheDocument();
});

test('the toggle switches the theme and remembers the choice', async () => {
    setSystemColorScheme(false);
    renderToggle();

    await userEvent.click(screen.getByRole('button', { name: 'Увімкнути темну тему' }));

    expect(currentMode()).toBe('dark');
    expect(window.localStorage.getItem(THEME_MODE_KEY)).toBe('dark');

    await userEvent.click(screen.getByRole('button', { name: 'Увімкнути світлу тему' }));
    expect(currentMode()).toBe('light');
    expect(window.localStorage.getItem(THEME_MODE_KEY)).toBe('light');
});

test('a remembered choice wins over the system setting, also after a reload', () => {
    setSystemColorScheme(true);
    window.localStorage.setItem(THEME_MODE_KEY, 'light');

    renderToggle();

    expect(currentMode()).toBe('light');
});

test('a value in storage that is not a theme is ignored', () => {
    setSystemColorScheme(true);
    window.localStorage.setItem(THEME_MODE_KEY, 'purple');

    renderToggle();

    expect(currentMode()).toBe('dark');
});

test('blocked storage does not break the toggle', async () => {
    setSystemColorScheme(false);
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
        throw new Error('storage is blocked');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
        throw new Error('storage is blocked');
    });

    renderToggle();
    await userEvent.click(screen.getByRole('button', { name: 'Увімкнути темну тему' }));

    expect(currentMode()).toBe('dark');
});

test('the page colour and the meta theme-color follow the theme', async () => {
    setSystemColorScheme(false);
    const meta = document.createElement('meta');
    meta.name = 'theme-color';
    meta.content = '#ffffff';
    document.head.appendChild(meta);

    renderToggle();
    const lightColor = meta.content;
    await userEvent.click(screen.getByRole('button', { name: 'Увімкнути темну тему' }));

    expect(meta.content).not.toBe(lightColor);
    expect(document.documentElement.style.backgroundColor).not.toBe('');
    meta.remove();
});
