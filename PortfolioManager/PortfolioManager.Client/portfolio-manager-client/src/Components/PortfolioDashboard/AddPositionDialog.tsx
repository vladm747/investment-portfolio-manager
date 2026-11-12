import React, { useEffect, useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import TextField from '@mui/material/TextField';
import { parseDecimal, parseWholeNumber, PositionForm, validatePosition } from '../../Domain/validation';
import { CreateStock } from '../../Services/StockService';
import { getErrorMessage } from '../../Services/errors';

const todayText = () => {
    const now = new Date();
    return [now.getFullYear(), String(now.getMonth() + 1).padStart(2, '0'), String(now.getDate()).padStart(2, '0')].join('-');
};

const emptyForm = (): PositionForm => ({ symbol: '', quantity: '', entryPrice: '', entryDate: todayText() });

interface AddPositionDialogProps {
    open: boolean;
    portfolioId: number;
    onClose: () => void;
    /** The position was saved on the server. */
    onAdded: (symbol: string) => void;
}

export default function AddPositionDialog({ open, portfolioId, onClose, onAdded }: AddPositionDialogProps) {
    const [form, setForm] = useState<PositionForm>(emptyForm);
    const [touched, setTouched] = useState<Partial<Record<keyof PositionForm, boolean>>>({});
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState<string | null>(null);

    useEffect(() => {
        if (open) {
            setForm(emptyForm());
            setTouched({});
            setServerError(null);
            setSubmitting(false);
        }
    }, [open]);

    const errors = validatePosition(form);
    const errorOf = (field: keyof PositionForm) => (touched[field] ? errors[field] : undefined);
    const bind = (field: keyof PositionForm) => ({
        value: form[field],
        onChange: (event: React.ChangeEvent<HTMLInputElement>) => setForm((current) => ({ ...current, [field]: event.target.value })),
        onBlur: () => setTouched((current) => ({ ...current, [field]: true })),
        error: Boolean(errorOf(field)),
        disabled: submitting,
    });

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        setTouched({ symbol: true, quantity: true, entryPrice: true, entryDate: true });
        if (Object.keys(errors).length > 0) {
            return;
        }
        const symbol = form.symbol.trim().toUpperCase();
        setServerError(null);
        setSubmitting(true);
        try {
            await CreateStock(portfolioId, {
                symbol,
                quantity: parseWholeNumber(form.quantity),
                entryPrice: parseDecimal(form.entryPrice),
                // A calendar day without a time zone, so it is the same day wherever the browser is.
                entryDate: `${form.entryDate}T00:00:00`,
            });
            onAdded(symbol);
        } catch (error) {
            setServerError(getErrorMessage(error, 'Не вдалося додати позицію. Перевірте тікер і спробуйте ще раз.'));
            setSubmitting(false);
        }
    };

    return (
        <Dialog open={open} onClose={submitting ? undefined : onClose} maxWidth="xs" fullWidth aria-labelledby="add-position-title">
            <form onSubmit={handleSubmit} noValidate>
                <DialogTitle id="add-position-title">Нова позиція</DialogTitle>
                <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '8px !important' }}>
                    <TextField
                        autoFocus
                        required
                        fullWidth
                        label="Тікер"
                        placeholder="AAPL"
                        autoComplete="off"
                        inputProps={{ maxLength: 10, autoCapitalize: 'characters', style: { textTransform: 'uppercase' } }}
                        helperText={errorOf('symbol') ?? 'Біржовий код компанії, наприклад AAPL або MSFT'}
                        {...bind('symbol')}
                    />
                    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                        <TextField
                            required
                            label="Кількість"
                            inputProps={{ inputMode: 'numeric' }}
                            helperText={errorOf('quantity') ?? 'Скільки акцій куплено'}
                            {...bind('quantity')}
                        />
                        <TextField
                            required
                            label="Середня ціна входу"
                            inputProps={{ inputMode: 'decimal' }}
                            helperText={errorOf('entryPrice') ?? 'За одну акцію'}
                            {...bind('entryPrice')}
                        />
                    </Box>
                    <TextField
                        required
                        fullWidth
                        type="date"
                        label="Дата входу"
                        InputLabelProps={{ shrink: true }}
                        inputProps={{ max: todayText() }}
                        helperText={errorOf('entryDate')}
                        {...bind('entryDate')}
                    />
                    {serverError && <Alert severity="error" role="alert">{serverError}</Alert>}
                </DialogContent>
                <DialogActions>
                    <Button onClick={onClose} disabled={submitting} color="inherit">Скасувати</Button>
                    <Button
                        type="submit"
                        variant="contained"
                        disabled={submitting}
                        startIcon={submitting ? <CircularProgress size={16} color="inherit" /> : undefined}
                    >
                        Додати
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    );
}
