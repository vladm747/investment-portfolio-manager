import Box from '@mui/material/Box';

/** The product mark: a rising line on an accent square. The same drawing is public/favicon.svg. */
export default function Logo({ size = 32 }: { size?: number }) {
    return (
        <Box
            aria-hidden
            sx={{
                width: size,
                height: size,
                flex: 'none',
                borderRadius: `${Math.round(size * 0.28)}px`,
                background: (theme) => theme.app.accent,
                color: (theme) => theme.app.onAccent,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
            }}
        >
            <svg width={size * 0.56} height={size * 0.56} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="3 17 9 11 13 15 21 7" />
                <polyline points="15 7 21 7 21 13" />
            </svg>
        </Box>
    );
}
