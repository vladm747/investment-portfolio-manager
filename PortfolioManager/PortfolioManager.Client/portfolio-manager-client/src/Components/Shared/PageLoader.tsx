import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';

/** Shown while a page's code is being downloaded. */
export default function PageLoader() {
    return (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '40vh' }}>
            <CircularProgress aria-label="Завантаження" />
        </Box>
    );
}
