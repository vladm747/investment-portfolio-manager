import { Location, Navigate, Outlet, useLocation } from "react-router-dom";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { useAuth } from "./AuthProvider";

function FullPageLoader() {
    return (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
            <CircularProgress aria-label="Завантаження" />
        </Box>
    );
}

/** Pages for signed-in users. Anyone else goes to the sign-in page and comes back here afterwards. */
export function RequireAuth() {
    const { status } = useAuth();
    const location = useLocation();

    if (status === 'loading') {
        return <FullPageLoader />;
    }

    if (status === 'anonymous') {
        return <Navigate to="/login" replace state={{ from: location }} />;
    }

    return <Outlet />;
}

/** Sign-in and sign-up. A signed-in user is sent to the page they wanted, or to the portfolio list. */
export function GuestOnly() {
    const { status } = useAuth();
    const location = useLocation();

    if (status === 'loading') {
        return <FullPageLoader />;
    }

    if (status === 'authenticated') {
        const from = (location.state as { from?: Location } | null)?.from;
        return <Navigate to={from ?? '/'} state={from?.state} replace />;
    }

    return <Outlet />;
}
