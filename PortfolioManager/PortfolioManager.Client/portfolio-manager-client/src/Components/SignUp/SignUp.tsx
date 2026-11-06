import * as React from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Link from '@mui/material/Link';
import TextField from '@mui/material/TextField';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import {useAuth} from "../../Auth/AuthProvider";
import {getErrorMessages} from "../../Services/errors";
import {usePageTitle} from "../../Hooks/usePageTitle";
import AuthLayout from "../Layout/AuthLayout";
import PasswordField from "../Shared/PasswordField";

// Must match the server: Identity password options in Program.cs and User.FullName's length.
const PASSWORD_HINT = 'Щонайменше 6 символів: велика й мала латинські літери, цифра та спецсимвол.';
const MAX_FULL_NAME_LENGTH = 50;

function SignUp() {
    usePageTitle('Реєстрація');
    const { signUp } = useAuth();
    const location = useLocation();
    const [errors, setErrors] = React.useState<string[]>([]);
    const [submitting, setSubmitting] = React.useState(false);

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const firstName = String(data.get('firstName') ?? '').trim();
        const lastName = String(data.get('lastName') ?? '').trim();
        const email = String(data.get('email') ?? '').trim();
        const password = String(data.get('password') ?? '');

        if (!firstName || !lastName || !email || !password) {
            setErrors(['Заповніть усі поля.']);
            return;
        }

        const fullName = `${firstName} ${lastName}`;
        if (fullName.length > MAX_FULL_NAME_LENGTH) {
            setErrors([`Ім'я та прізвище разом мають бути не довші за ${MAX_FULL_NAME_LENGTH} символів.`]);
            return;
        }

        setErrors([]);
        setSubmitting(true);
        try {
            // The new account is signed in straight away; the route guard then opens the portfolio list.
            await signUp({ fullName, email, password });
        } catch (e) {
            setErrors(getErrorMessages(e, 'Не вдалося зареєструватися. Спробуйте ще раз.'));
            setSubmitting(false);
        }
    };

    return (
        <AuthLayout
            title="Реєстрація"
            subtitle="Створіть акаунт, щоб вести свої портфелі"
            footer={
                <>
                    Вже маєте акаунт?{' '}
                    <Link component={RouterLink} to="/login" state={location.state}>
                        Увійти
                    </Link>
                </>
            }
        >
            <Box component="form" noValidate onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                    <TextField
                        autoComplete="given-name"
                        name="firstName"
                        required
                        fullWidth
                        id="firstName"
                        label="Ім'я"
                        autoFocus
                        disabled={submitting}
                    />
                    <TextField
                        required
                        fullWidth
                        id="lastName"
                        label="Прізвище"
                        name="lastName"
                        autoComplete="family-name"
                        disabled={submitting}
                    />
                </Box>
                <TextField
                    required
                    fullWidth
                    id="email"
                    label="Пошта"
                    name="email"
                    type="email"
                    autoComplete="email"
                    disabled={submitting}
                />
                <PasswordField
                    required
                    fullWidth
                    name="password"
                    label="Пароль"
                    id="password"
                    autoComplete="new-password"
                    helperText={PASSWORD_HINT}
                    disabled={submitting}
                />
                {errors.length > 0 && (
                    <Alert severity="error" role="alert">
                        {errors.length === 1 ? errors[0] : (
                            <Box component="ul" sx={{ m: 0, pl: 2 }}>
                                {errors.map((message) => <li key={message}>{message}</li>)}
                            </Box>
                        )}
                    </Alert>
                )}
                <Button
                    type="submit"
                    fullWidth
                    variant="contained"
                    disabled={submitting}
                    startIcon={submitting ? <CircularProgress size={18} color="inherit" /> : undefined}
                    sx={{ mt: 1 }}
                >
                    {submitting ? 'Реєструємо…' : 'Зареєструватися'}
                </Button>
            </Box>
        </AuthLayout>
    );
}
export default SignUp;
