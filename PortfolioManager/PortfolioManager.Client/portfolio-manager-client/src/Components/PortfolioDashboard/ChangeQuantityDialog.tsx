import React, { useEffect, useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import TextField from '@mui/material/TextField';
import StockDto from '../../DTO/StockDto';
import { parseWholeNumber, validateQuantity } from '../../Domain/validation';
import { UpdateStockQuantity } from '../../Services/StockService';
import { getErrorMessage } from '../../Services/errors';

interface ChangeQuantityDialogProps {
    /** The position being edited; the dialog is open while this is set. */
    stock: StockDto | null;
    onClose: () => void;
    /** The new quantity was saved on the server. */
    onSaved: (stock: StockDto, quantity: number) => void;
}

export default function ChangeQuantityDialog({ stock, onClose, onSaved }: ChangeQuantityDialogProps) {
    // What the user typed; until they type, the field shows the position's current quantity.
    const [typed, setTyped] = useState<string | null>(null);
    const quantity = typed ?? String(stock?.quantity ?? '');
    const [touched, setTouched] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState<string | null>(null);

    useEffect(() => {
        if (stock) {
            setTyped(null);
            setTouched(false);
            setServerError(null);
            setSubmitting(false);
        }
    }, [stock]);

    const validationError = validateQuantity(quantity);
    const showError = touched ? validationError : null;

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (!stock) return;
        setTouched(true);
        if (validationError) return;

        const next = parseWholeNumber(quantity);
        if (next === stock.quantity) {
            onClose();
            return;
        }
        setServerError(null);
        setSubmitting(true);
        try {
            await UpdateStockQuantity(stock.id, next);
            onSaved(stock, next);
        } catch (error) {
            setServerError(getErrorMessage(error, 'Не вдалося змінити кількість. Спробуйте ще раз.'));
            setSubmitting(false);
        }
    };

    return (
        <Dialog open={stock !== null} onClose={submitting ? undefined : onClose} maxWidth="xs" fullWidth aria-labelledby="change-quantity-title">
            <form onSubmit={handleSubmit} noValidate>
                <DialogTitle id="change-quantity-title">Змінити кількість {stock?.symbol}</DialogTitle>
                <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '8px !important' }}>
                    <TextField
                        autoFocus
                        required
                        fullWidth
                        label="Кількість акцій"
                        value={quantity}
                        onChange={(event) => setTyped(event.target.value)}
                        onBlur={() => setTouched(true)}
                        error={Boolean(showError)}
                        helperText={showError ?? 'Вартість і прибуток позиції буде перераховано за поточною ціною.'}
                        inputProps={{ inputMode: 'numeric' }}
                        disabled={submitting}
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
                        Зберегти
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    );
}
