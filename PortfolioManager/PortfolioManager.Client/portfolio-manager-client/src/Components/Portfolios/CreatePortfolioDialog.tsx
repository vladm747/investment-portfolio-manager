import React, { useEffect, useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import TextField from '@mui/material/TextField';
import { getErrorMessage } from '../../Services/errors';

/** Portfolio.Name is limited to 100 characters in the database. */
export const MAX_PORTFOLIO_NAME_LENGTH = 100;

export function validatePortfolioName(name: string): string | null {
    const trimmed = name.trim();
    if (!trimmed) return 'Вкажіть назву портфеля.';
    if (trimmed.length > MAX_PORTFOLIO_NAME_LENGTH) return `Назва має бути не довшою за ${MAX_PORTFOLIO_NAME_LENGTH} символів.`;
    return null;
}

interface CreatePortfolioDialogProps {
    open: boolean;
    onClose: () => void;
    /** Creates the portfolio; throws if the server refuses, and the message is shown in the dialog. */
    onCreate: (name: string) => Promise<void>;
}

export default function CreatePortfolioDialog({ open, onClose, onCreate }: CreatePortfolioDialogProps) {
    const [name, setName] = useState('');
    const [touched, setTouched] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [serverError, setServerError] = useState<string | null>(null);

    // A fresh form every time the dialog opens.
    useEffect(() => {
        if (open) {
            setName('');
            setTouched(false);
            setServerError(null);
            setSubmitting(false);
        }
    }, [open]);

    const validationError = validatePortfolioName(name);
    const showError = touched && validationError;

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        setTouched(true);
        if (validationError) {
            return;
        }
        setServerError(null);
        setSubmitting(true);
        try {
            await onCreate(name.trim());
        } catch (error) {
            setServerError(getErrorMessage(error, 'Не вдалося створити портфель. Спробуйте ще раз.'));
            setSubmitting(false);
        }
    };

    return (
        <Dialog open={open} onClose={submitting ? undefined : onClose} maxWidth="xs" fullWidth aria-labelledby="create-portfolio-title">
            <form onSubmit={handleSubmit} noValidate>
                <DialogTitle id="create-portfolio-title">Новий портфель</DialogTitle>
                <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '8px !important' }}>
                    <TextField
                        autoFocus
                        fullWidth
                        required
                        label="Назва портфеля"
                        placeholder="Наприклад, Довгострокові інвестиції"
                        value={name}
                        onChange={(event) => setName(event.target.value)}
                        onBlur={() => setTouched(true)}
                        error={Boolean(showError)}
                        helperText={showError || `${name.trim().length} / ${MAX_PORTFOLIO_NAME_LENGTH}`}
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
                        Створити
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    );
}
