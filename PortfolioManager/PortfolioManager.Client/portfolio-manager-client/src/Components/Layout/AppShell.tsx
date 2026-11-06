import Box from '@mui/material/Box';
import { Suspense } from 'react';
import { Outlet } from 'react-router-dom';
import PageLoader from '../Shared/PageLoader';
import TopBar, { CONTENT_MAX_WIDTH } from './TopBar';

/** Top bar plus the page area: every page after sign-in, and the 404 page, sit inside it. */
export default function AppShell() {
    return (
        <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
            <Box
                component="a"
                href="#main-content"
                sx={(theme) => ({
                    position: 'absolute',
                    left: 16,
                    top: -64,
                    zIndex: theme.zIndex.tooltip,
                    px: 2,
                    py: 1,
                    borderRadius: 1,
                    background: theme.app.accent,
                    color: theme.app.onAccent,
                    fontWeight: 600,
                    textDecoration: 'none',
                    '&:focus': { top: 8 },
                })}
            >
                Перейти до вмісту
            </Box>
            <TopBar />
            <Box
                component="main"
                id="main-content"
                tabIndex={-1}
                sx={{
                    flex: 1,
                    width: '100%',
                    maxWidth: CONTENT_MAX_WIDTH,
                    mx: 'auto',
                    boxSizing: 'border-box',
                    px: { xs: 2, md: 4 },
                    pt: { xs: 3, md: 4 },
                    pb: 7,
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 3,
                    outline: 'none',
                }}
            >
                <Suspense fallback={<PageLoader />}>
                    <Outlet />
                </Suspense>
            </Box>
        </Box>
    );
}
