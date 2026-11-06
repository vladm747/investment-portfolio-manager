import React from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Typography from '@mui/material/Typography';
import CheckCircleOutline from '@mui/icons-material/CheckCircleOutline';
import ThemeToggle from '../../Theme/ThemeToggle';
import { PRODUCT_NAME } from '../../Hooks/usePageTitle';
import Delta from '../Shared/Delta';
import Logo from './Logo';

const BENEFITS = [
    'Вартість і прибуток усіх портфелів на одному екрані',
    'Динаміка зростання та розподіл за секторами',
    'Оптимізація портфеля за моделлю Марковіца',
];

/** A decorative preview of the dashboard; the figures are illustrative and hidden from screen readers. */
function PreviewCard() {
    return (
        <Card aria-hidden sx={{ p: 2.5, maxWidth: 380, display: 'flex', flexDirection: 'column', gap: 1 }}>
            <Typography variant="subtitle2" color="text.secondary">Вартість портфеля</Typography>
            <Typography variant="kpi" component="p" sx={{ m: 0 }}>16 846,20 $</Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Delta value={34.76} format="percent" />
                <Typography variant="caption" color="text.disabled">за весь час</Typography>
            </Box>
            <Box component="svg" viewBox="0 0 320 90" width="100%" sx={{ mt: 1, display: 'block' }}>
                <Box component="path" d="M0 76 L24 70 L48 72 L72 60 L96 64 L120 50 L144 54 L168 40 L192 44 L216 30 L240 34 L264 20 L288 24 L320 10 L320 90 L0 90 Z" sx={{ fill: (theme) => theme.app.accent, fillOpacity: 0.14 }} />
                <Box component="path" d="M0 76 L24 70 L48 72 L72 60 L96 64 L120 50 L144 54 L168 40 L192 44 L216 30 L240 34 L264 20 L288 24 L320 10" sx={{ fill: 'none', stroke: (theme) => theme.app.accent, strokeWidth: 2, strokeLinejoin: 'round', strokeLinecap: 'round' }} />
            </Box>
        </Card>
    );
}

interface AuthLayoutProps {
    title: string;
    subtitle?: string;
    children: React.ReactNode;
    /** The line under the form: a link to the other auth page. */
    footer: React.ReactNode;
}

/** One layout for sign-in and sign-up: a brand panel beside the form (the form alone on a phone). */
export default function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
    return (
        <Box
            sx={{
                minHeight: '100vh',
                display: 'grid',
                gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 5fr) minmax(0, 6fr)' },
            }}
        >
            <Box
                component="aside"
                sx={(theme) => ({
                    display: { xs: 'none', md: 'flex' },
                    flexDirection: 'column',
                    justifyContent: 'space-between',
                    gap: 6,
                    p: 6,
                    background: theme.app.accentSoft,
                })}
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25 }}>
                    <Logo />
                    <Typography variant="subtitle1" component="span" fontWeight={600}>{PRODUCT_NAME}</Typography>
                </Box>

                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                    <Typography component="p" sx={{ fontSize: '2rem', lineHeight: '2.5rem', fontWeight: 600, letterSpacing: '-0.02em', maxWidth: 420 }}>
                        Ваші інвестиції — під контролем
                    </Typography>
                    <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                        {BENEFITS.map((text) => (
                            <Box component="li" key={text} sx={{ display: 'flex', gap: 1.25, alignItems: 'flex-start' }}>
                                <CheckCircleOutline sx={{ fontSize: 20, mt: '1px', color: (theme) => theme.app.accentText }} />
                                <Typography>{text}</Typography>
                            </Box>
                        ))}
                    </Box>
                </Box>

                <PreviewCard />
            </Box>

            <Box component="main" sx={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}>
                <Box sx={{ display: 'flex', justifyContent: { xs: 'space-between', md: 'flex-end' }, alignItems: 'center', p: 2 }}>
                    <Box sx={{ display: { xs: 'flex', md: 'none' }, alignItems: 'center', gap: 1.25 }}>
                        <Logo />
                        <Typography variant="subtitle1" component="span" fontWeight={600}>{PRODUCT_NAME}</Typography>
                    </Box>
                    <ThemeToggle />
                </Box>

                <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', px: 2, pb: 8 }}>
                    <Box sx={{ width: '100%', maxWidth: 400, display: 'flex', flexDirection: 'column', gap: 3 }}>
                        <Box>
                            <Typography variant="h1" component="h1">{title}</Typography>
                            {subtitle && <Typography color="text.secondary" sx={{ mt: 0.5 }}>{subtitle}</Typography>}
                        </Box>
                        {children}
                        <Typography variant="body2" color="text.secondary" align="center">{footer}</Typography>
                    </Box>
                </Box>
            </Box>
        </Box>
    );
}
