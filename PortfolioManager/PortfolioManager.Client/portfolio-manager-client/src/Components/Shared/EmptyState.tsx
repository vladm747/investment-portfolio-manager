import React from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

interface EmptyStateProps {
    /** An icon element, e.g. <WorkOutline/>. */
    icon: React.ReactNode;
    title: string;
    description?: string;
    /** Usually the button that fixes the emptiness. */
    action?: React.ReactNode;
}

/** What a list or a chart shows when there is nothing in it yet, and what to do about it. */
export default function EmptyState({ icon, title, description, action }: EmptyStateProps) {
    return (
        <Box
            sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                textAlign: 'center',
                gap: 1,
                py: 6,
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
                    background: (theme) => theme.app.accentSoft,
                    color: (theme) => theme.app.accentText,
                    mb: 1,
                }}
            >
                {icon}
            </Box>
            <Typography component="h2" variant="h2">
                {title}
            </Typography>
            {description && (
                <Typography color="text.secondary" sx={{ maxWidth: 420 }}>
                    {description}
                </Typography>
            )}
            {action && <Box sx={{ mt: 1.5 }}>{action}</Box>}
        </Box>
    );
}
