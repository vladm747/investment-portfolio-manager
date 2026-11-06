import React, { createContext, useCallback, useContext, useMemo, useState } from 'react';
import Alert, { AlertColor } from '@mui/material/Alert';
import Snackbar from '@mui/material/Snackbar';

interface Notification {
    key: number;
    message: string;
    severity: AlertColor;
}

interface NotifyApi {
    success: (message: string) => void;
    error: (message: string) => void;
    info: (message: string) => void;
}

const NotificationsContext = createContext<NotifyApi | null>(null);

/** Short messages after an action («Портфель створено»). One at a time; a new one replaces the old. */
export function NotificationsProvider({ children }: { children: React.ReactNode }) {
    const [current, setCurrent] = useState<Notification | null>(null);
    const [open, setOpen] = useState(false);

    const show = useCallback((severity: AlertColor, message: string) => {
        setCurrent({ key: Date.now(), message, severity });
        setOpen(true);
    }, []);

    const api = useMemo<NotifyApi>(
        () => ({
            success: (message) => show('success', message),
            error: (message) => show('error', message),
            info: (message) => show('info', message),
        }),
        [show]
    );

    const handleClose = (_: unknown, reason?: string) => {
        if (reason !== 'clickaway') {
            setOpen(false);
        }
    };

    return (
        <NotificationsContext.Provider value={api}>
            {children}
            <Snackbar
                key={current?.key}
                open={open}
                autoHideDuration={current?.severity === 'error' ? 8000 : 5000}
                onClose={handleClose}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                {current ? (
                    <Alert
                        severity={current.severity}
                        variant="filled"
                        onClose={() => setOpen(false)}
                        closeText="Закрити"
                        sx={{ width: '100%', boxShadow: (theme) => theme.app.menuShadow }}
                    >
                        {current.message}
                    </Alert>
                ) : undefined}
            </Snackbar>
        </NotificationsContext.Provider>
    );
}

export function useNotify(): NotifyApi {
    const context = useContext(NotificationsContext);
    if (!context) {
        throw new Error('useNotify must be used inside <NotificationsProvider>');
    }
    return context;
}
