import React from 'react';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';

interface ConfirmDialogProps {
    open: boolean;
    title: string;
    /** What will happen; for a deletion, say what is lost. */
    message: React.ReactNode;
    confirmLabel?: string;
    cancelLabel?: string;
    /** Draws the confirm button in the loss colour: the action cannot be undone. */
    danger?: boolean;
    /** The action is running: the buttons are disabled and the dialog cannot be dismissed. */
    loading?: boolean;
    onConfirm: () => void;
    onClose: () => void;
}

/** Asks «are you sure?» before something that cannot be undone. */
export default function ConfirmDialog({
    open, title, message, confirmLabel = 'Підтвердити', cancelLabel = 'Скасувати', danger, loading, onConfirm, onClose,
}: ConfirmDialogProps) {
    return (
        <Dialog
            open={open}
            onClose={loading ? undefined : onClose}
            maxWidth="xs"
            fullWidth
            aria-labelledby="confirm-dialog-title"
            aria-describedby="confirm-dialog-message"
        >
            <DialogTitle id="confirm-dialog-title">{title}</DialogTitle>
            <DialogContent>
                <DialogContentText id="confirm-dialog-message" component="div">
                    {message}
                </DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} disabled={loading} color="inherit">
                    {cancelLabel}
                </Button>
                <Button
                    variant="contained"
                    color={danger ? 'error' : 'primary'}
                    onClick={onConfirm}
                    disabled={loading}
                    startIcon={loading ? <CircularProgress size={16} color="inherit" /> : undefined}
                    autoFocus
                >
                    {confirmLabel}
                </Button>
            </DialogActions>
        </Dialog>
    );
}
