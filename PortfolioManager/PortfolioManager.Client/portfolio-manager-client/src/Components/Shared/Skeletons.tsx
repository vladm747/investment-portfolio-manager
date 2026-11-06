import Box from '@mui/material/Box';
import Skeleton from '@mui/material/Skeleton';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableRow from '@mui/material/TableRow';
import { KpiGrid } from './KpiCard';
import KpiCard from './KpiCard';

/** Placeholders keep the page the same size while data loads, so nothing jumps. */

export function KpiRowSkeleton({ count = 4 }: { count?: number }) {
    return (
        <KpiGrid>
            {Array.from({ length: count }, (_, index) => (
                <KpiCard key={index} label=" " value="" loading />
            ))}
        </KpiGrid>
    );
}

export function TableSkeleton({ rows = 5, columns = 5 }: { rows?: number; columns?: number }) {
    return (
        <Table aria-busy="true" aria-label="Завантаження">
            <TableBody>
                {Array.from({ length: rows }, (_, row) => (
                    <TableRow key={row}>
                        {Array.from({ length: columns }, (_, column) => (
                            <TableCell key={column}>
                                <Skeleton width={column === 0 ? '80%' : '60%'} sx={{ ml: column === 0 ? 0 : 'auto' }} />
                            </TableCell>
                        ))}
                    </TableRow>
                ))}
            </TableBody>
        </Table>
    );
}

export function ChartSkeleton({ height = 276 }: { height?: number }) {
    return (
        <Box aria-busy="true" aria-label="Завантаження" role="status" sx={{ height }}>
            <Skeleton variant="rounded" width="100%" height="100%" />
        </Box>
    );
}
