import React from 'react';
import Box from '@mui/material/Box';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Link from '@mui/material/Link';
import Typography from '@mui/material/Typography';
import ChevronRight from '@mui/icons-material/ChevronRight';
import { Link as RouterLink } from 'react-router-dom';

export interface Crumb {
    label: string;
    /** Missing for the current page, the last crumb. */
    to?: string;
}

interface PageHeaderProps {
    title: string;
    subtitle?: React.ReactNode;
    breadcrumbs?: Crumb[];
    /** Buttons on the right; they wrap under the title on a phone. */
    actions?: React.ReactNode;
}

/** The top of every page: breadcrumbs, the title with a line under it, and the page's actions. */
export default function PageHeader({ title, subtitle, breadcrumbs, actions }: PageHeaderProps) {
    return (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            {breadcrumbs && breadcrumbs.length > 0 && (
                <Breadcrumbs
                    aria-label="Навігаційний ланцюжок"
                    separator={<ChevronRight sx={{ fontSize: 16, color: 'text.disabled' }} />}
                    sx={{ fontSize: '0.8125rem' }}
                >
                    {breadcrumbs.map((crumb) =>
                        crumb.to ? (
                            <Link
                                key={crumb.label}
                                component={RouterLink}
                                to={crumb.to}
                                underline="hover"
                                sx={{ color: 'text.secondary', fontWeight: 400 }}
                            >
                                {crumb.label}
                            </Link>
                        ) : (
                            <Typography key={crumb.label} variant="body2" color="text.primary" fontWeight={500} aria-current="page">
                                {crumb.label}
                            </Typography>
                        )
                    )}
                </Breadcrumbs>
            )}

            <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', justifyContent: 'space-between', gap: 2 }}>
                <Box sx={{ minWidth: 0 }}>
                    <Typography variant="h1" sx={{ overflowWrap: 'anywhere' }}>
                        {title}
                    </Typography>
                    {subtitle && (
                        <Typography color="text.secondary" component="div" sx={{ mt: 0.5, display: 'flex', alignItems: 'center', gap: 0.75 }}>
                            {subtitle}
                        </Typography>
                    )}
                </Box>
                {actions && <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>{actions}</Box>}
            </Box>
        </Box>
    );
}
