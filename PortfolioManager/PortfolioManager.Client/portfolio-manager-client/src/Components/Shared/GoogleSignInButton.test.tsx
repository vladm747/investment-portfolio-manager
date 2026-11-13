import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ThemeToggle from '../../Theme/ThemeToggle';
import { installFakeGoogle, removeFakeGoogle } from '../../testing/fakeApi';
import { renderWithProviders, setSystemColorScheme } from '../../testing/renderWithProviders';
import GoogleSignInButton from './GoogleSignInButton';

const CLIENT_ID = 'test-client.apps.googleusercontent.com';

const renderButton = (onCredential: (credential: string) => void = () => undefined, disabled = false) =>
    renderWithProviders(
        <>
            <ThemeToggle />
            <GoogleSignInButton onCredential={onCredential} disabled={disabled} />
        </>
    );

beforeEach(() => {
    window.localStorage.clear();
    setSystemColorScheme(false);
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', CLIENT_ID);
});

afterEach(() => {
    removeFakeGoogle();
    document.querySelectorAll('script[src*="accounts.google.com"]').forEach((script) => script.remove());
    vi.unstubAllEnvs();
});

test('Google is set up once with the application client ID and draws its official button', async () => {
    const google = installFakeGoogle();

    renderButton();

    await waitFor(() => expect(google.buttons).toHaveLength(1));
    expect(google.initialisations).toHaveLength(1);
    expect(google.initialisations[0].client_id).toBe(CLIENT_ID);
    expect(google.buttons[0].options).toMatchObject({ type: 'standard', size: 'large', text: 'signin_with', locale: 'uk' });
});

test('the button is drawn in the light theme and drawn again in the dark one, without setting Google up again', async () => {
    const google = installFakeGoogle();
    renderButton();
    await waitFor(() => expect(google.buttons).toHaveLength(1));
    expect(google.buttons[0].options.theme).toBe('outline');

    await userEvent.click(screen.getByRole('button', { name: 'Увімкнути темну тему' }));

    await waitFor(() => expect(google.buttons).toHaveLength(2));
    expect(google.buttons[1].options.theme).toBe('filled_black');
    expect(screen.getByTestId('google-button')).toHaveAttribute('data-google-theme', 'filled_black');
    expect(google.id.initialize).toHaveBeenCalledTimes(1);
});

test('the button starts in the dark theme when the system prefers it', async () => {
    setSystemColorScheme(true);
    const google = installFakeGoogle();

    renderButton();

    await waitFor(() => expect(google.buttons).toHaveLength(1));
    expect(google.buttons[0].options.theme).toBe('filled_black');
});

test('the ID token Google hands over reaches the page', async () => {
    const onCredential = vi.fn();
    const google = installFakeGoogle();
    renderButton(onCredential);
    await waitFor(() => expect(google.buttons).toHaveLength(1));

    google.chooseAccount('google-id-token');

    expect(onCredential).toHaveBeenCalledExactlyOnceWith('google-id-token');
});

test('a response without a token is ignored', async () => {
    const onCredential = vi.fn();
    const google = installFakeGoogle();
    renderButton(onCredential);
    await waitFor(() => expect(google.buttons).toHaveLength(1));

    google.chooseAccount(undefined);

    expect(onCredential).not.toHaveBeenCalled();
});

test('the newest handler gets the token even though Google keeps the callback of its first setup', async () => {
    const google = installFakeGoogle();
    const first = renderButton(vi.fn());
    await waitFor(() => expect(google.buttons).toHaveLength(1));
    first.unmount();

    const second = vi.fn();
    renderButton(second);
    await waitFor(() => expect(google.buttons).toHaveLength(2));
    google.chooseAccount('token');

    expect(google.id.initialize).toHaveBeenCalledTimes(1);
    expect(second).toHaveBeenCalledWith('token');
});

test('a disabled button ignores clicks and is dimmed', async () => {
    const google = installFakeGoogle();
    renderButton(() => undefined, true);
    await waitFor(() => expect(google.buttons).toHaveLength(1));

    const style = getComputedStyle(screen.getByTestId('google-button').parentElement!);

    expect(style.pointerEvents).toBe('none');
    expect(style.opacity).toBe('0.5');
});

test('without a client ID the page says Google sign-in is not set up, and never loads Google', () => {
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', '');

    renderButton();

    expect(screen.getByText(/ще не налаштовано/)).toBeInTheDocument();
    expect(document.querySelector('script[src*="accounts.google.com"]')).toBeNull();
});

// Last on purpose: a script that has loaded stays loaded for the rest of this file.
test('when Google cannot be loaded the page says so and loading can be tried again', async () => {
    renderButton();
    const firstScript = await waitFor(() => {
        const script = document.querySelector('script[src="https://accounts.google.com/gsi/client"]');
        expect(script).not.toBeNull();
        return script!;
    });

    firstScript.dispatchEvent(new Event('error')); // blocked by an ad blocker, or offline

    expect(await screen.findByText(/Не вдалося завантажити вхід через Google/)).toBeInTheDocument();
    expect(document.querySelector('script[src*="accounts.google.com"]')).toBeNull();

    await userEvent.click(screen.getByRole('button', { name: 'Спробувати ще раз' }));
    const secondScript = await waitFor(() => {
        const script = document.querySelector('script[src="https://accounts.google.com/gsi/client"]');
        expect(script).not.toBeNull();
        return script!;
    });
    const google = installFakeGoogle(); // the network is back: the script defines window.google as it loads
    secondScript.dispatchEvent(new Event('load'));

    await waitFor(() => expect(google.buttons).toHaveLength(1));
});
