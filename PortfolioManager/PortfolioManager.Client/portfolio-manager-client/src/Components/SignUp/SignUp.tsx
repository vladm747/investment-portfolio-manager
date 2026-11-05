import * as React from 'react';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import CssBaseline from '@mui/material/CssBaseline';
import TextField from '@mui/material/TextField';
import Grid from '@mui/material/Grid';
import Box from '@mui/material/Box';
import Link from '@mui/material/Link';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import Typography from '@mui/material/Typography';
import Container from '@mui/material/Container';
import { createTheme, ThemeProvider } from '@mui/material/styles';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import {useAuth} from "../../Auth/AuthProvider";
import {getErrorMessages} from "../../Services/errors";

function Copyright(props: any) {
    return (
        <Typography variant="body2" color="text.secondary" align="center" {...props}>
            {'Copyright © '}
            {new Date().getFullYear()}
            {'.'}
        </Typography>
    );
}

// TODO remove, this demo shouldn't need to reset the theme.
const defaultTheme = createTheme();

// Must match the server: Identity password options in Program.cs and User.FullName's length.
const PASSWORD_HINT = 'Щонайменше 6 символів: велика й мала латинські літери, цифра та спецсимвол.';
const MAX_FULL_NAME_LENGTH = 50;

function SignUp() {
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
        <ThemeProvider theme={defaultTheme}>
            <Container component="main" maxWidth="xs">
                <CssBaseline />
                <Box
                    sx={{
                        marginTop: 8,
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                    }}
                >
                    <Avatar sx={{ m: 1, bgcolor: 'secondary.main' }}>
                        <LockOutlinedIcon />
                    </Avatar>
                    <Typography component="h1" variant="h5">
                        Реєстрація
                    </Typography>
                    <Box component="form" noValidate onSubmit={handleSubmit} sx={{ mt: 3 }}>
                        <Grid container spacing={2}>
                            <Grid item xs={12} sm={6}>
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
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    required
                                    fullWidth
                                    id="lastName"
                                    label="Прізвище"
                                    name="lastName"
                                    autoComplete="family-name"
                                    disabled={submitting}
                                />
                            </Grid>
                            <Grid item xs={12}>
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
                            </Grid>
                            <Grid item xs={12}>
                                <TextField
                                    required
                                    fullWidth
                                    name="password"
                                    label="Пароль"
                                    type="password"
                                    id="password"
                                    autoComplete="new-password"
                                    helperText={PASSWORD_HINT}
                                    disabled={submitting}
                                />
                            </Grid>
                        </Grid>
                        {errors.length > 0 && (
                            <Alert severity="error" sx={{ mt: 2 }} role="alert">
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
                            sx={{ mt: 3, mb: 2 }}
                        >
                            {submitting ? 'Реєструємо…' : 'Зареєструватись'}
                        </Button>
                        <Typography variant="body2" align="center">
                            Вже маєте акаунт?{' '}
                            <Link component={RouterLink} to="/login" state={location.state}>
                                Увійти
                            </Link>
                        </Typography>
                    </Box>
                </Box>
                <Copyright sx={{ mt: 5 }} />
            </Container>
        </ThemeProvider>
    );
}
export default SignUp;
