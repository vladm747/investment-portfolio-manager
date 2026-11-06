import React from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Skeleton from '@mui/material/Skeleton';
import Typography from '@mui/material/Typography';

interface KpiCardProps {
    label: string;
    /** The big figure. Pass text; long names are cut with an ellipsis. */
    value: React.ReactNode;
    /** Usually a <Delta/>. */
    delta?: React.ReactNode;
    /** Small grey text next to the delta. */
    note?: React.ReactNode;
    loading?: boolean;
    /** Smaller figure for values that are words (a portfolio name) rather than amounts. */
    textValue?: boolean;
}

/** One headline figure: a label, the value, and how it changed. */
export default function KpiCard({ label, value, delta, note, loading, textValue }: KpiCardProps) {
    return (
        <Card component="section" sx={{ p: { xs: 2, sm: 2.5 }, display: 'flex', flexDirection: 'column', gap: 1, minWidth: 0 }}>
            <Typography component="h2" variant="subtitle2" color="text.secondary">
                {label}
            </Typography>

            {loading ? (
                <>
                    <Skeleton variant="rounded" width="65%" height={36} />
                    <Skeleton variant="rounded" width="40%" height={24} />
                </>
            ) : (
                <>
                    <Typography
                        variant="kpi"
                        component="p"
                        title={typeof value === 'string' ? value : undefined}
                        sx={{
                            m: 0,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            // Smaller on a phone, where two cards share a row.
                            fontSize: textValue ? '1.125rem' : { xs: '1.25rem', sm: '1.75rem' },
                            lineHeight: { xs: '1.75rem', sm: '2.25rem' },
                        }}
                    >
                        {value}
                    </Typography>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', columnGap: 1, rowGap: 0.5, minHeight: 24, minWidth: 0 }}>
                        {delta}
                        {note && (
                            <Typography
                                variant="caption"
                                color="text.disabled"
                                sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                            >
                                {note}
                            </Typography>
                        )}
                    </Box>
                </>
            )}
        </Card>
    );
}

/** A row of KPI cards: two columns on a phone, otherwise as many as fit, never narrower than 240px. */
export function KpiGrid({ children }: { children: React.ReactNode }) {
    return (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(auto-fit, minmax(240px, 1fr))' }, gap: { xs: 1.5, sm: 2 } }}>
            {children}
        </Box>
    );
}
