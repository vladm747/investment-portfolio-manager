import Button from '@mui/material/Button';
import SearchOff from '@mui/icons-material/SearchOff';
import { Link as RouterLink } from 'react-router-dom';
import { usePageTitle } from '../../Hooks/usePageTitle';
import EmptyState from '../Shared/EmptyState';

export default function NotFoundPage() {
    usePageTitle('Сторінку не знайдено');

    return (
        <EmptyState
            icon={<SearchOff />}
            title="Сторінку не знайдено"
            description="Можливо, адресу введено з помилкою або сторінку видалено."
            action={<Button variant="contained" component={RouterLink} to="/">До портфелів</Button>}
        />
    );
}
