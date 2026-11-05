import * as React from 'react';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import CssBaseline from '@mui/material/CssBaseline';
import TextField from '@mui/material/TextField';
import Paper from '@mui/material/Paper';
import Box from '@mui/material/Box';
import Grid from '@mui/material/Grid';
import Link from '@mui/material/Link';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import Typography from '@mui/material/Typography';
import {createTheme, ThemeProvider} from '@mui/material/styles';
import {Link as RouterLink, useLocation} from "react-router-dom";
import {useAuth} from "../../Auth/AuthProvider";
import {getErrorMessage} from "../../Services/errors";

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

function SignIn() {
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
        <ThemeProvider theme={defaultTheme}>
            <Grid container component="main" sx={{ height: '92vh' }}>
                <CssBaseline />
                <Grid
                    item
                    xs={false}
                    sm={4}
                    md={7}
                    sx={{
                        backgroundImage: `url(${process.env.PUBLIC_URL + '/background.jpeg'})`,
                        backgroundRepeat: 'no-repeat',
                        backgroundColor: (t) =>
                            t.palette.mode === 'light' ? t.palette.grey[50] : t.palette.grey[900],
                        backgroundSize: 'cover',
                        backgroundPosition: 'center',
                    }}
                />
                <Grid item xs={12} sm={8} md={5} component={Paper} elevation={6} square>
                    <Box
                        sx={{
                            my: 8,
                            mx: 4,
                            display: 'flex',
                            flexDirection: 'column',
                            alignItems: 'center',
                        }}
                    >
                        <Avatar sx={{ m: 1, bgcolor: 'secondary.main' }}>
                            <LockOutlinedIcon />
                        </Avatar>
                        <Typography component="h1" variant="h5">
                            Вхід
                        </Typography>
                        <Box component="form" noValidate onSubmit={handleSubmit} sx={{ mt: 1, width: '100%' }}>
                            <TextField
                                margin="normal"
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
                            <TextField
                                margin="normal"
                                required
                                fullWidth
                                name="password"
                                label="Пароль"
                                type="password"
                                id="password"
                                autoComplete="current-password"
                                disabled={submitting}
                            />
                            {error && (
                                <Alert severity="error" sx={{ mt: 2 }} role="alert">
                                    {error}
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
                                {submitting ? 'Входимо…' : 'Увійти'}
                            </Button>
                            <Typography variant="body2" align="center">
                                Немає акаунта?{' '}
                                <Link component={RouterLink} to="/sign-up" state={location.state}>
                                    Зареєструватися
                                </Link>
                            </Typography>
                            <Copyright sx={{ mt: 5 }} />
                        </Box>
                    </Box>
                </Grid>
            </Grid>
        </ThemeProvider>
    );
}

export default SignIn;
