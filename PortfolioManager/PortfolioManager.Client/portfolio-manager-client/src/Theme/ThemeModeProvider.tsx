import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { createAppTheme, pageBackground, ThemeMode } from './theme';

/** Where the user's choice is remembered. Without a choice the app follows the system setting. */
export const THEME_MODE_KEY = 'portfolioManager.themeMode';

const DARK_QUERY = '(prefers-color-scheme: dark)';

function readStoredMode(): ThemeMode | null {
    try {
        const value = window.localStorage.getItem(THEME_MODE_KEY);
        return value === 'light' || value === 'dark' ? value : null;
    } catch {
        // Storage blocked by the browser: the choice then lasts until the page is closed.
        return null;
    }
}

function writeStoredMode(mode: ThemeMode) {
    try {
        window.localStorage.setItem(THEME_MODE_KEY, mode);
    } catch {
        // Nothing to do: the choice still applies to this visit.
    }
}

const systemMedia = () =>
    typeof window !== 'undefined' && typeof window.matchMedia === 'function' ? window.matchMedia(DARK_QUERY) : null;

interface ThemeModeContextValue {
    mode: ThemeMode;
    toggleMode: () => void;
}

const ThemeModeContext = createContext<ThemeModeContextValue | null>(null);

/** The only ThemeProvider and CssBaseline of the app; also owns the light/dark choice. */
export function ThemeModeProvider({ children }: { children: React.ReactNode }) {
    const [stored, setStored] = useState<ThemeMode | null>(readStoredMode);
    const [systemDark, setSystemDark] = useState(() => systemMedia()?.matches ?? false);

    useEffect(() => {
        const media = systemMedia();
        if (!media) {
            return;
        }
        const onChange = (event: MediaQueryListEvent) => setSystemDark(event.matches);
        media.addEventListener('change', onChange);
        return () => media.removeEventListener('change', onChange);
    }, []);

    // Another tab changed the choice.
    useEffect(() => {
        const onStorage = (event: StorageEvent) => {
            if (event.key === THEME_MODE_KEY) {
                setStored(readStoredMode());
            }
        };
        window.addEventListener('storage', onStorage);
        return () => window.removeEventListener('storage', onStorage);
    }, []);

    const mode: ThemeMode = stored ?? (systemDark ? 'dark' : 'light');

    const toggleMode = useCallback(() => {
        const next: ThemeMode = mode === 'dark' ? 'light' : 'dark';
        setStored(next);
        writeStoredMode(next);
    }, [mode]);

    const theme = useMemo(() => createAppTheme(mode), [mode]);

    // Keeps the area outside the app (overscroll, the address bar on phones) in the page colour.
    useEffect(() => {
        document.documentElement.dataset.theme = mode;
        document.documentElement.style.backgroundColor = pageBackground(mode);
        document.querySelector('meta[name="theme-color"]')?.setAttribute('content', pageBackground(mode));
    }, [mode]);

    const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode]);

    return (
        <ThemeModeContext.Provider value={value}>
            <ThemeProvider theme={theme}>
                <CssBaseline enableColorScheme />
                {children}
            </ThemeProvider>
        </ThemeModeContext.Provider>
    );
}

export function useThemeMode(): ThemeModeContextValue {
    const context = useContext(ThemeModeContext);
    if (!context) {
        throw new Error('useThemeMode must be used inside <ThemeModeProvider>');
    }
    return context;
}
