import * as React from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Link from '@mui/material/Link';
import Typography from '@mui/material/Typography';
import {Link as RouterLink} from "react-router-dom";
import {useAuth} from "../../Auth/AuthProvider";
import {getErrorMessage} from "../../Services/errors";
import {usePageTitle} from "../../Hooks/usePageTitle";
import AuthLayout from "../Layout/AuthLayout";
import GoogleSignInButton from "../Shared/GoogleSignInButton";

/** The only way in: there are no passwords. A first sign-in creates the account. */
function SignIn() {
    usePageTitle('Вхід');
    const { signInWithGoogle } = useAuth();
    const [error, setError] = React.useState<string | null>(null);
    const [submitting, setSubmitting] = React.useState(false);

    const handleCredential = async (credential: string) => {
        setError(null);
        setSubmitting(true);
        try {
            // On success the route guard takes the user to the page they wanted, or to the portfolio list.
            await signInWithGoogle(credential);
        } catch (e) {
            setError(getErrorMessage(e, 'Не вдалося увійти. Спробуйте ще раз.'));
            setSubmitting(false);
        }
    };

    return (
        <AuthLayout
            title="Вхід"
            subtitle="Увійдіть через акаунт Google, щоб побачити свої портфелі"
            footer={
                <>
                    Входячи, ви погоджуєтесь з тим, як ми обробляємо дані:{' '}
                    <Link component={RouterLink} to="/privacy">
                        політика конфіденційності
                    </Link>
                </>
            }
        >
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <GoogleSignInButton onCredential={handleCredential} disabled={submitting} />
                {submitting && (
                    <Typography variant="body2" color="text.secondary" align="center" role="status">
                        Входимо…
                    </Typography>
                )}
                {error && (
                    <Alert severity="error" role="alert">
                        {error}
                    </Alert>
                )}
                <Typography variant="body2" color="text.secondary" align="center">
                    Окремого пароля немає. Акаунт створюється автоматично під час першого входу.
                </Typography>
            </Box>
        </AuthLayout>
    );
}

export default SignIn;
