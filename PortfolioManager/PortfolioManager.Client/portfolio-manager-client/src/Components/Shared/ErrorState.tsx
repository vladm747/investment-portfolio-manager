import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import ErrorOutline from '@mui/icons-material/ErrorOutline';
import Refresh from '@mui/icons-material/Refresh';

interface ErrorStateProps {
    /** Already in plain Ukrainian, e.g. from getErrorMessage(). */
    message: string;
    title?: string;
    /** Shows the «Спробувати ще раз» button. */
    onRetry?: () => void;
    /** Less padding, for inside a card next to other content. */
    compact?: boolean;
}

/** A failed load: what happened and a way to try again. */
export default function ErrorState({ message, title = 'Не вдалося завантажити дані', onRetry, compact }: ErrorStateProps) {
    return (
        <Box
            role="alert"
            sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                textAlign: 'center',
                gap: 1,
                py: compact ? 3 : 6,
                px: 3,
            }}
        >
            <Box
                aria-hidden
                sx={{
                    width: 48,
                    height: 48,
                    borderRadius: '50%',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    background: (theme) => theme.app.lossSoft,
                    color: (theme) => theme.app.loss,
                    mb: 1,
                }}
            >
                <ErrorOutline />
            </Box>
            <Typography component="h2" variant="h2">
                {title}
            </Typography>
            <Typography color="text.secondary" sx={{ maxWidth: 460 }}>
                {message}
            </Typography>
            {onRetry && (
                <Button variant="outlined" startIcon={<Refresh />} onClick={onRetry} sx={{ mt: 1.5 }}>
                    Спробувати ще раз
                </Button>
            )}
        </Box>
    );
}
