import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import { Link as RouterLink } from 'react-router-dom';
import { usePageTitle } from '../../Hooks/usePageTitle';
import ErrorState from './ErrorState';

/** For an address that names no portfolio: a malformed id, or one the server does not know. */
export default function PortfolioNotFound() {
    usePageTitle('Портфель не знайдено');
    return (
        <Card>
            <ErrorState
                title="Портфель не знайдено"
                message="Перевірте адресу або поверніться до списку портфелів: можливо, цей портфель видалено."
            />
            <Box sx={{ display: 'flex', justifyContent: 'center', pb: 4, mt: -3 }}>
                <Button variant="contained" component={RouterLink} to="/">До портфелів</Button>
            </Box>
        </Card>
    );
}
