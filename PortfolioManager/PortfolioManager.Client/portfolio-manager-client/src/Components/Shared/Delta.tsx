import Box from '@mui/material/Box';
import { useTheme } from '@mui/material/styles';
import { visuallyHidden } from '@mui/utils';
import ArrowUpward from '@mui/icons-material/ArrowUpward';
import ArrowDownward from '@mui/icons-material/ArrowDownward';
import Remove from '@mui/icons-material/Remove';
import { Direction, directionOf, formatNumber, formatMoney, formatPercent } from '../../Format/format';

interface DeltaProps {
    value: number | null | undefined;
    /**
     * `percent` expects percent units (5.12 for 5,12%); `points` is a change of a percentage («+2,92 п.п.»);
     * `number` is a plain signed figure such as a change of a ratio.
     */
    format: 'percent' | 'money' | 'points' | 'number';
    currency?: string;
    /** `chip` has a tinted background and an arrow; `text` is just the signed figure, for dense tables. */
    variant?: 'chip' | 'text';
    /** Draw the arrow in `text` variant too. */
    showIcon?: boolean;
    /** Digits after the comma. */
    digits?: number;
    /**
     * Which direction is good news. For a portfolio's value it is `up`; for volatility or a drawdown a fall is
     * the good one (`down`), so it is drawn in the gain colour while the arrow still points down.
     */
    good?: 'up' | 'down';
}

const WORDS: Record<Direction, string> = { up: 'Зростання', down: 'Спад', flat: 'Без змін' };

function signedText({ value, format, currency, digits }: Pick<DeltaProps, 'format' | 'currency'> & { value: number; digits: number }): string {
    switch (format) {
        case 'percent':
            return formatPercent(value, { signed: true, digits });
        case 'money':
            return formatMoney(value, { signed: true, currency, digits });
        case 'points':
            return `${formatPercent(value, { signed: true, digits }).replace('%', '')} п.п.`;
        case 'number': {
            const text = formatNumber(Math.abs(value), digits);
            return Number(value.toFixed(digits)) === 0 ? text : `${value > 0 ? '+' : '−'}${text}`;
        }
    }
}

/**
 * A gain or a loss. It never relies on colour alone: the figure carries «+» or «−», the arrow points the way,
 * and a screen reader hears the direction in words.
 */
export default function Delta({ value, format, currency, variant = 'chip', showIcon, digits = 2, good = 'up' }: DeltaProps) {
    const { app } = useTheme();

    if (value === null || value === undefined || Number.isNaN(value)) {
        return <Box component="span" sx={{ color: 'text.disabled' }}>—</Box>;
    }

    const direction = directionOf(value, digits);
    const text = signedText({ value, format, currency, digits });

    const tone = direction === 'flat' ? 'flat' : (direction === good ? 'up' : 'down');
    const color = { up: app.gain, down: app.loss, flat: app.flat }[tone];
    const background = { up: app.gainSoft, down: app.lossSoft, flat: app.flatSoft }[tone];
    const Icon = { up: ArrowUpward, down: ArrowDownward, flat: Remove }[direction];
    const withIcon = variant === 'chip' || showIcon;

    return (
        <Box
            component="span"
            sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 0.5,
                color,
                fontWeight: variant === 'chip' ? 600 : 500,
                fontVariantNumeric: 'tabular-nums',
                whiteSpace: 'nowrap',
                ...(variant === 'chip' && {
                    background,
                    height: 24,
                    px: 1,
                    borderRadius: 999,
                    fontSize: '0.8125rem',
                }),
            }}
        >
            {withIcon && <Icon aria-hidden sx={{ fontSize: 14 }} />}
            <span style={visuallyHidden}>{WORDS[direction]} </span>
            {text}
        </Box>
    );
}
