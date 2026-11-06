import { alpha, createTheme, Theme } from '@mui/material/styles';
import { ukUA } from '@mui/material/locale';
import type { CSSProperties } from 'react';

export type ThemeMode = 'light' | 'dark';

/** Colours the MUI palette has no slot for. Components read them from `theme.app`. */
export interface AppTokens {
    /** Cards and the top bar. */
    surface: string;
    /** Table headers, chips, the segmented control. */
    surface2: string;
    /** Row under the pointer, open menu button. */
    hover: string;
    border: string;
    borderStrong: string;
    textMuted: string;
    accent: string;
    accentHover: string;
    onAccent: string;
    accentSoft: string;
    accentText: string;
    gain: string;
    gainSoft: string;
    loss: string;
    lossSoft: string;
    /** Zero change: neither gain nor loss. */
    flat: string;
    flatSoft: string;
    cardShadow: string;
    menuShadow: string;
    chart: {
        grid: string;
        axis: string;
        /** The "before" series next to the accent one, e.g. current against optimised weights. */
        neutral: string;
        /** Eight categorical colours in a fixed order; checked for colour-blind separation in both modes. */
        categorical: string[];
        /** Sectors beyond the eight colours. */
        other: string;
    };
}

const light: AppTokens = {
    surface: '#FFFFFF',
    surface2: '#F2F4F7',
    hover: '#F9FAFB',
    border: '#E4E7EC',
    borderStrong: '#D0D5DD',
    textMuted: '#5B677A',
    accent: '#4F46E5',
    accentHover: '#4338CA',
    onAccent: '#FFFFFF',
    accentSoft: '#EEF0FF',
    accentText: '#4338CA',
    gain: '#067647',
    gainSoft: '#ECFDF3',
    loss: '#B42318',
    lossSoft: '#FEF3F2',
    flat: '#475467',
    flatSoft: '#F2F4F7',
    cardShadow: '0 1px 2px rgba(16,24,40,0.05)',
    menuShadow: '0 16px 40px rgba(16,24,40,0.16)',
    chart: {
        grid: '#EAECF0',
        axis: '#D0D5DD',
        neutral: '#7D8595',
        categorical: ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300', '#6250d6', '#e34948'],
        other: '#98A2B3',
    },
};

const dark: AppTokens = {
    surface: '#13161B',
    surface2: '#1A1E25',
    hover: '#171B21',
    border: '#232831',
    borderStrong: '#343A45',
    textMuted: '#8A93A1',
    accent: '#818CF8',
    accentHover: '#A5B4FC',
    onAccent: '#0C0E12',
    accentSoft: 'rgba(129,140,248,0.14)',
    accentText: '#A5B4FC',
    gain: '#47CD89',
    gainSoft: 'rgba(71,205,137,0.12)',
    loss: '#F97066',
    lossSoft: 'rgba(249,112,102,0.12)',
    flat: '#A9B0BC',
    flatSoft: 'rgba(169,176,188,0.12)',
    cardShadow: 'none',
    menuShadow: '0 16px 40px rgba(0,0,0,0.55)',
    chart: {
        grid: '#232831',
        axis: '#343A45',
        neutral: '#7A8394',
        categorical: ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#008300', '#9085e9', '#e66767'],
        other: '#6B7280',
    },
};

const text = {
    light: { primary: '#101828', secondary: '#475467' },
    dark: { primary: '#F2F4F7', secondary: '#A9B0BC' },
};

const page = { light: '#F6F7F9', dark: '#0C0E12' };

/** The page colour, also used by index.html before React has started. */
export const pageBackground = (mode: ThemeMode) => page[mode];

const FONT_FAMILY = '"Inter Variable", "Inter", system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';

declare module '@mui/material/styles' {
    interface Theme {
        app: AppTokens;
    }
    interface ThemeOptions {
        app?: AppTokens;
    }
    interface TypographyVariants {
        /** The big figure of a KPI card. */
        kpi: CSSProperties;
    }
    interface TypographyVariantsOptions {
        kpi?: CSSProperties;
    }
}

declare module '@mui/material/Typography' {
    interface TypographyPropsVariantOverrides {
        kpi: true;
    }
}

export function createAppTheme(mode: ThemeMode): Theme {
    const t = mode === 'dark' ? dark : light;
    const ink = text[mode];

    return createTheme(
        {
            app: t,
            palette: {
                mode,
                primary: { main: t.accent, dark: t.accentHover, contrastText: t.onAccent },
                secondary: { main: ink.secondary },
                error: { main: t.loss, contrastText: mode === 'dark' ? '#0C0E12' : '#FFFFFF' },
                success: { main: t.gain },
                warning: { main: mode === 'dark' ? '#FEC84B' : '#B54708' },
                info: { main: t.accentText },
                text: { primary: ink.primary, secondary: ink.secondary, disabled: t.textMuted },
                background: { default: page[mode], paper: t.surface },
                divider: t.border,
                action: {
                    hover: alpha(ink.primary, 0.05),
                    selected: t.accentSoft,
                    focus: alpha(ink.primary, 0.08),
                },
            },
            shape: { borderRadius: 8 },
            typography: {
                fontFamily: FONT_FAMILY,
                fontWeightRegular: 400,
                fontWeightMedium: 500,
                fontWeightBold: 600,
                h1: { fontSize: '1.75rem', lineHeight: '2.25rem', fontWeight: 600, letterSpacing: '-0.02em' },
                h2: { fontSize: '1rem', lineHeight: '1.5rem', fontWeight: 600 },
                h3: { fontSize: '0.875rem', lineHeight: '1.25rem', fontWeight: 600 },
                h4: { fontSize: '0.875rem', lineHeight: '1.25rem', fontWeight: 600 },
                h5: { fontSize: '0.875rem', lineHeight: '1.25rem', fontWeight: 600 },
                h6: { fontSize: '0.875rem', lineHeight: '1.25rem', fontWeight: 600 },
                subtitle1: { fontSize: '1rem', lineHeight: '1.5rem', fontWeight: 500 },
                subtitle2: { fontSize: '0.8125rem', lineHeight: '1rem', fontWeight: 500 },
                body1: { fontSize: '0.875rem', lineHeight: '1.25rem' },
                body2: { fontSize: '0.8125rem', lineHeight: '1.25rem' },
                caption: { fontSize: '0.75rem', lineHeight: '1rem' },
                button: { fontSize: '0.875rem', lineHeight: '1.25rem', fontWeight: 600, textTransform: 'none' },
                kpi: {
                    fontSize: '1.75rem',
                    lineHeight: '2.25rem',
                    fontWeight: 600,
                    letterSpacing: '-0.02em',
                    fontVariantNumeric: 'tabular-nums',
                },
            },
            components: {
                MuiCssBaseline: {
                    styleOverrides: {
                        body: { fontFeatureSettings: '"cv11"' },
                        // Keyboard focus is always visible; the mouse does not draw a ring.
                        ':focus-visible': { outline: `2px solid ${t.accent}`, outlineOffset: 2 },
                        '::selection': { background: t.accentSoft },
                    },
                },
                // ButtonBase sets `outline: 0` and shows only a faint ripple on keyboard focus; draw a real ring instead.
                MuiButtonBase: {
                    styleOverrides: {
                        root: { '&.Mui-focusVisible': { outline: `2px solid ${t.accent}`, outlineOffset: 2 } },
                    },
                },
                MuiButton: {
                    defaultProps: { disableElevation: true },
                    styleOverrides: {
                        root: { borderRadius: 8, minHeight: 40, padding: '8px 16px' },
                        sizeSmall: { minHeight: 32, padding: '4px 12px' },
                        outlined: {
                            color: ink.primary,
                            borderColor: t.borderStrong,
                            '&:hover': { borderColor: t.borderStrong, background: t.surface2 },
                        },
                        outlinedError: { color: t.loss, borderColor: t.loss, '&:hover': { borderColor: t.loss, background: t.lossSoft } },
                        text: { '&:hover': { background: t.accentSoft } },
                        textError: { '&:hover': { background: t.lossSoft } },
                    },
                },
                MuiIconButton: { styleOverrides: { root: { borderRadius: 8 } } },
                MuiPaper: { styleOverrides: { root: { backgroundImage: 'none' } } },
                MuiCard: {
                    defaultProps: { variant: 'outlined' },
                    styleOverrides: { root: { borderRadius: 12, borderColor: t.border, boxShadow: t.cardShadow } },
                },
                MuiAppBar: {
                    defaultProps: { elevation: 0, color: 'inherit' },
                    styleOverrides: { root: { background: t.surface, color: ink.primary, borderBottom: `1px solid ${t.border}` } },
                },
                // Hidden labels inside a table are absolutely positioned; without this they escape the scroll box and widen the page.
                MuiTableContainer: { styleOverrides: { root: { position: 'relative' } } },
                MuiTableCell: {
                    styleOverrides: {
                        root: { borderBottom: `1px solid ${t.border}`, padding: '12px 16px', fontVariantNumeric: 'tabular-nums' },
                        head: {
                            background: t.surface2,
                            color: ink.secondary,
                            fontSize: '0.75rem',
                            lineHeight: '1rem',
                            fontWeight: 500,
                            padding: '10px 16px',
                            whiteSpace: 'nowrap',
                        },
                    },
                },
                MuiTableRow: {
                    styleOverrides: {
                        root: { '&:last-child > td, &:last-child > th': { borderBottom: 0 } },
                        hover: { '&:hover': { background: t.hover } },
                    },
                },
                MuiTableSortLabel: {
                    styleOverrides: {
                        root: { '&.Mui-active': { color: t.accentText }, '&:hover': { color: t.accentText } },
                        icon: { fontSize: 16 },
                    },
                },
                MuiChip: {
                    styleOverrides: {
                        root: { borderRadius: 6, fontWeight: 500 },
                        sizeSmall: { height: 24 },
                    },
                },
                MuiTooltip: {
                    defaultProps: { arrow: true },
                    styleOverrides: {
                        tooltip: {
                            background: ink.primary,
                            color: page[mode],
                            fontSize: '0.75rem',
                            lineHeight: '1rem',
                            padding: '8px 10px',
                            borderRadius: 8,
                            maxWidth: 280,
                        },
                        arrow: { color: ink.primary },
                    },
                },
                MuiOutlinedInput: {
                    styleOverrides: {
                        root: { borderRadius: 8, background: t.surface },
                        notchedOutline: { borderColor: t.borderStrong },
                        // MUI fades placeholders to about 2.5:1; the muted text colour keeps them above AA (4.5:1).
                        input: { '&::placeholder': { color: t.textMuted, opacity: 1 } },
                    },
                },
                MuiDialog: { styleOverrides: { paper: { borderRadius: 12, border: `1px solid ${t.border}`, boxShadow: t.menuShadow } } },
                MuiDialogTitle: { styleOverrides: { root: { fontSize: '1.125rem', lineHeight: '1.75rem', fontWeight: 600, padding: '20px 24px 8px' } } },
                MuiDialogActions: { styleOverrides: { root: { padding: '16px 24px 20px', gap: 4 } } },
                MuiMenu: {
                    styleOverrides: {
                        paper: { borderRadius: 10, border: `1px solid ${t.border}`, boxShadow: t.menuShadow, minWidth: 200 },
                        list: { padding: 6 },
                    },
                },
                MuiMenuItem: {
                    styleOverrides: {
                        // Inside the menu the ring is drawn inwards, so the menu's edge does not clip it.
                        root: { minHeight: 36, borderRadius: 6, fontSize: '0.875rem', '&.Mui-focusVisible': { outlineOffset: -2 } },
                    },
                },
                MuiAlert: { styleOverrides: { root: { borderRadius: 8, alignItems: 'center' } } },
                MuiLink: { defaultProps: { underline: 'hover' }, styleOverrides: { root: { color: t.accentText, fontWeight: 500 } } },
                MuiSkeleton: { styleOverrides: { root: { backgroundColor: alpha(ink.primary, mode === 'dark' ? 0.1 : 0.07) } } },
                MuiToggleButtonGroup: {
                    styleOverrides: {
                        root: { background: t.surface2, padding: 3, borderRadius: 8, gap: 2 },
                        grouped: {
                            border: 0,
                            borderRadius: '6px !important',
                            margin: 0,
                        },
                    },
                },
                MuiToggleButton: {
                    styleOverrides: {
                        root: {
                            height: 28,
                            minWidth: 40,
                            padding: '0 10px',
                            fontSize: '0.75rem',
                            fontWeight: 600,
                            color: ink.secondary,
                            '&:hover': { background: alpha(ink.primary, 0.06) },
                            '&.Mui-selected': {
                                background: t.surface,
                                color: ink.primary,
                                boxShadow: mode === 'dark' ? `0 0 0 1px ${t.borderStrong}` : '0 1px 2px rgba(16,24,40,0.12)',
                                '&:hover': { background: t.surface },
                            },
                        },
                    },
                },
            },
        },
        ukUA
    );
}
