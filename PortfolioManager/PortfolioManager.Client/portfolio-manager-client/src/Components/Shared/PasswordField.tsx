import { useState } from 'react';
import IconButton from '@mui/material/IconButton';
import InputAdornment from '@mui/material/InputAdornment';
import TextField, { TextFieldProps } from '@mui/material/TextField';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';

/** A password field with a button that shows or hides what was typed. */
export default function PasswordField(props: Omit<TextFieldProps, 'type'>) {
    const [visible, setVisible] = useState(false);
    const label = visible ? 'Сховати пароль' : 'Показати пароль';

    return (
        <TextField
            {...props}
            type={visible ? 'text' : 'password'}
            InputProps={{
                ...props.InputProps,
                endAdornment: (
                    <InputAdornment position="end">
                        <IconButton
                            aria-label={label}
                            title={label}
                            edge="end"
                            size="small"
                            disabled={props.disabled}
                            onClick={() => setVisible((value) => !value)}
                            // Keeps the caret in the field when the button is pressed with the mouse.
                            onMouseDown={(event) => event.preventDefault()}
                        >
                            {visible ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                        </IconButton>
                    </InputAdornment>
                ),
            }}
        />
    );
}
