import * as React from 'react';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import CssBaseline from '@mui/material/CssBaseline';
import TextField from '@mui/material/TextField';
import Paper from '@mui/material/Paper';
import Box from '@mui/material/Box';
import Grid from '@mui/material/Grid';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import Typography from '@mui/material/Typography';
import { createTheme, ThemeProvider } from '@mui/material/styles';
import {SignInAsync} from "../../Services/AuthService";
import SignInDto  from "../../Components/SignIn/SignInDto";
import {useNavigate} from "react-router-dom";
import {useCookies} from "react-cookie";
import UserDto from "../../DTO/UserDto";

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
    const navigate = useNavigate();
    const [cookie, setCookie] = useCookies(['accessToken'])
    const [currenUser, setCurrentUser] = React.useState<UserDto>({} as UserDto);

    // const fetchCurrentUser = async () => {
    //     const token = cookie.accessToken;
    //     if (token) {
    //         const response = GetCurrentUser(cookie.accessToken)
    //         response.then((data)=>{
    //             setCurrentUser(data);
    //         }).catch((error) => {
    //             console.error('SignIn. Error fetching current user', error);
    //             throw error;
    //         });
    //
    //         if(currenUser && currenUser.id != ''){
    //             setAuthenticated(true);
    //         }
    //     }
    // };

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const signInDto: SignInDto = {
            email: data.get('email') as string,
            password: data.get('password') as string
        };
        console.log({
            email: data.get('email'),
            password: data.get('password'),
        });

        const tokens = await SignInAsync(signInDto);

        if (tokens.accessToken && tokens.refreshToken) {
            setCookie('accessToken', tokens.accessToken);


            navigate('/');
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
                            Логін
                        </Typography>
                        <Box component="form" noValidate onSubmit={handleSubmit} sx={{ mt: 1 }}>
                            <TextField
                                margin="normal"
                                required
                                fullWidth
                                id="email"
                                label="Пошта"
                                name="email"
                                autoComplete="email"
                                autoFocus
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
                            />
                            <Button
                                type="submit"
                                fullWidth
                                variant="contained"
                                sx={{ mt: 3, mb: 2 }}
                            >
                                Увійти
                            </Button>
                            <Copyright sx={{ mt: 5 }} />
                        </Box>
                    </Box>
                </Grid>
            </Grid>
        </ThemeProvider>
    );
}

export default SignIn;
