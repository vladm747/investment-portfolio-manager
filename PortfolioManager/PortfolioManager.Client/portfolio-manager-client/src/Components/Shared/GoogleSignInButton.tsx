import { useEffect, useRef, useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Skeleton from '@mui/material/Skeleton';
import { useThemeMode } from '../../Theme/ThemeModeProvider';

/**
 * Google Identity Services, the only part of Google's library this app uses: it draws the official "Sign in with
 * Google" button and hands the page an ID token. The token is then sent to our API (see SignInWithGoogleAsync), which
 * checks it with Google. There is no npm package: the script comes from Google, as Google's terms ask.
 */
const GOOGLE_SCRIPT_URL = 'https://accounts.google.com/gsi/client';

/** Google allows a button between 200 and 400 px wide. */
const MIN_BUTTON_WIDTH = 200;
const MAX_BUTTON_WIDTH = 400;

interface CredentialResponse {
    /** The ID token (a JWT signed by Google). */
    credential?: string;
}

interface GoogleButtonOptions {
    type: 'standard';
    theme: 'outline' | 'filled_blue' | 'filled_black';
    size: 'large' | 'medium' | 'small';
    text: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
    shape: 'rectangular' | 'pill';
    logo_alignment: 'left' | 'center';
    locale: string;
    width?: number;
}

interface GoogleIdentityApi {
    initialize: (config: { client_id: string; callback: (response: CredentialResponse) => void }) => void;
    renderButton: (parent: HTMLElement, options: GoogleButtonOptions) => void;
}

declare global {
    interface Window {
        google?: { accounts?: { id?: GoogleIdentityApi } };
    }
}

let scriptRequest: Promise<void> | null = null;

/** Adds Google's script to the page once; resolves at once when it is already there. A failure can be retried. */
function loadGoogleScript(): Promise<void> {
    if (window.google?.accounts?.id) {
        return Promise.resolve();
    }
    if (!scriptRequest) {
        scriptRequest = new Promise<void>((resolve, reject) => {
            const script = document.createElement('script');
            script.src = GOOGLE_SCRIPT_URL;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => {
                script.remove();
                scriptRequest = null;
                reject(new Error('Google sign-in script failed to load'));
            };
            document.head.appendChild(script);
        });
    }
    return scriptRequest;
}

/** Whoever is showing the button right now; Google keeps the callback it was initialised with, so it calls this. */
const activeHandler: { current: ((credential: string) => void) | null } = { current: null };

// initialize() must be called once per page (Google warns otherwise), however often the button is shown.
const initialised = new WeakMap<object, string>();

function initialiseOnce(google: GoogleIdentityApi, clientId: string) {
    if (initialised.get(google) === clientId) {
        return;
    }
    google.initialize({
        client_id: clientId,
        callback: (response) => {
            if (response.credential) {
                activeHandler.current?.(response.credential);
            }
        },
    });
    initialised.set(google, clientId);
}

interface GoogleSignInButtonProps {
    /** Called with Google's ID token when the user has chosen an account. */
    onCredential: (credential: string) => void;
    /** Dims the button and ignores clicks, e.g. while the token is being exchanged. */
    disabled?: boolean;
}

/**
 * The official button. Google draws it in its own iframe, so it cannot be restyled (nor may its logo be altered);
 * it is drawn again with a matching theme when the user switches between light and dark.
 */
export default function GoogleSignInButton({ onCredential, disabled }: GoogleSignInButtonProps) {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID?.trim();
    const { mode } = useThemeMode();
    const [status, setStatus] = useState<'loading' | 'ready' | 'failed'>('loading');
    const [attempt, setAttempt] = useState(0);
    const containerRef = useRef<HTMLDivElement>(null);

    const onCredentialRef = useRef(onCredential);
    onCredentialRef.current = onCredential;

    useEffect(() => {
        const handler = (credential: string) => onCredentialRef.current(credential);
        activeHandler.current = handler;
        return () => {
            if (activeHandler.current === handler) {
                activeHandler.current = null;
            }
        };
    }, []);

    useEffect(() => {
        if (!clientId) {
            return;
        }
        let cancelled = false;
        setStatus('loading');
        loadGoogleScript().then(
            () => {
                const google = window.google?.accounts?.id;
                if (cancelled) {
                    return;
                }
                if (!google) {
                    setStatus('failed');
                    return;
                }
                initialiseOnce(google, clientId);
                setStatus('ready');
            },
            () => {
                if (!cancelled) {
                    setStatus('failed');
                }
            }
        );
        return () => {
            cancelled = true;
        };
    }, [clientId, attempt]);

    useEffect(() => {
        const container = containerRef.current;
        const google = window.google?.accounts?.id;
        if (status !== 'ready' || !container || !google) {
            return;
        }
        container.replaceChildren();
        const available = Math.floor(container.clientWidth);
        google.renderButton(container, {
            type: 'standard',
            theme: mode === 'dark' ? 'filled_black' : 'outline',
            size: 'large',
            text: 'signin_with',
            shape: 'rectangular',
            logo_alignment: 'left',
            locale: 'uk',
            ...(available >= MIN_BUTTON_WIDTH ? { width: Math.min(available, MAX_BUTTON_WIDTH) } : {}),
        });
    }, [status, mode]);

    if (!clientId) {
        return (
            <Alert severity="warning">
                Вхід через Google ще не налаштовано: не задано VITE_GOOGLE_CLIENT_ID.
            </Alert>
        );
    }

    if (status === 'failed') {
        return (
            <Alert
                severity="warning"
                action={
                    <Button color="inherit" size="small" onClick={() => setAttempt((n) => n + 1)}>
                        Спробувати ще раз
                    </Button>
                }
            >
                Не вдалося завантажити вхід через Google. Перевірте з’єднання та вимкніть блокувальник реклами для цього сайту.
            </Alert>
        );
    }

    return (
        <Box sx={{ minHeight: 40, opacity: disabled ? 0.5 : 1, pointerEvents: disabled ? 'none' : 'auto', transition: 'opacity 0.15s' }}>
            {status === 'loading' && (
                <Skeleton variant="rounded" height={40} aria-busy="true" aria-label="Завантаження кнопки Google" />
            )}
            {status === 'ready' && (
                <Box ref={containerRef} data-testid="google-button" sx={{ width: '100%', display: 'flex', justifyContent: 'center' }} />
            )}
        </Box>
    );
}
