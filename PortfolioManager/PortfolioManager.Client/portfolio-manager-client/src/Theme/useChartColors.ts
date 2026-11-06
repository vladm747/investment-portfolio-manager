import { useTheme } from '@mui/material/styles';

/** Everything a Recharts chart needs to look right in the current theme. */
export function useChartColors() {
    const theme = useTheme();
    const { app } = theme;

    return {
        accent: app.accent,
        neutral: app.chart.neutral,
        grid: app.chart.grid,
        axis: app.chart.axis,
        categorical: app.chart.categorical,
        otherCategory: app.chart.other,
        /** Axis labels and legends. */
        text: theme.palette.text.secondary,
        mutedText: app.textMuted,
        primaryText: theme.palette.text.primary,
        surface: app.surface,
        border: app.border,
        gain: app.gain,
        loss: app.loss,
        fontFamily: theme.typography.fontFamily,
    };
}

export type ChartColors = ReturnType<typeof useChartColors>;
