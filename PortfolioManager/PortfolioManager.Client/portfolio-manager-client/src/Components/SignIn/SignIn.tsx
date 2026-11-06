import * as React from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Link from '@mui/material/Link';
import TextField from '@mui/material/TextField';
import {Link as RouterLink, useLocation} from "react-router-dom";
import {useAuth} from "../../Auth/AuthProvider";
import {getErrorMessage} from "../../Services/errors";
import {usePageTitle} from "../../Hooks/usePageTitle";
import AuthLayout from "../Layout/AuthLayout";
import PasswordField from "../Shared/PasswordField";

function SignIn() {
    usePageTitle('Вхід');
    const { signIn } = useAuth();
    const location = useLocation();
    const [error, setError] = React.useState<string | null>(null);
    const [submitting, setSubmitting] = React.useState(false);

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const email = String(data.get('email') ?? '').trim();
        const password = String(data.get('password') ?? '');

        if (!email || !password) {
            setError('Вкажіть пошту та пароль.');
            return;
        }

        setError(null);
        setSubmitting(true);
        try {
            // On success the route guard takes the user to the page they wanted, or to the portfolio list.
            await signIn({ email, password });
        } catch (e) {
            setError(getErrorMessage(e, 'Не вдалося увійти. Спробуйте ще раз.'));
            setSubmitting(false);
        }
    };

    return (
        <AuthLayout
            title="Вхід"
            subtitle="Увійдіть, щоб побачити свої портфелі"
            footer={
                <>
                    Немає акаунта?{' '}
                    <Link component={RouterLink} to="/sign-up" state={location.state}>
                        Зареєструватися
                    </Link>
                </>
            }
        >
            <Box component="form" noValidate onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                    required
                    fullWidth
                    id="email"
                    label="Пошта"
                    name="email"
                    type="email"
                    autoComplete="email"
                    autoFocus
                    disabled={submitting}
                />
                <PasswordField
                    required
                    fullWidth
                    name="password"
                    label="Пароль"
                    id="password"
                    autoComplete="current-password"
                    disabled={submitting}
                />
                {error && (
                    <Alert severity="error" role="alert">
                        {error}
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
                    {submitting ? 'Входимо…' : 'Увійти'}
                </Button>
            </Box>
        </AuthLayout>
    );
}

export default SignIn;
