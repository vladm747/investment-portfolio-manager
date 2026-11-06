import { useState } from 'react';
import { visuallyHidden } from '@mui/utils';
import AppBar from '@mui/material/AppBar';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import ListItemIcon from '@mui/material/ListItemIcon';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import ExpandMore from '@mui/icons-material/ExpandMore';
import Logout from '@mui/icons-material/Logout';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../Auth/AuthProvider';
import ThemeToggle from '../../Theme/ThemeToggle';
import { PRODUCT_NAME } from '../../Hooks/usePageTitle';
import Logo from './Logo';

export const CONTENT_MAX_WIDTH = 1200;

function initialsOf(fullName: string, email: string): string {
    const letters = fullName.trim().split(/\s+/).filter(Boolean).slice(0, 2).map((word) => word[0]);
    return (letters.length ? letters.join('') : email.slice(0, 1)).toUpperCase();
}

/** Product name, the one section («Портфелі»), the theme switch and the account menu. */
export default function TopBar() {
    const { status, user, signOut } = useAuth();
    const navigate = useNavigate();
    const { pathname } = useLocation();
    const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);

    const inPortfolios = pathname === '/' || pathname.startsWith('/portfolios');

    const handleSignOut = () => {
        setMenuAnchor(null);
        signOut();
        navigate('/login', { replace: true });
    };

    return (
        <AppBar position="sticky" component="header">
            <Toolbar
                sx={{ width: '100%', maxWidth: CONTENT_MAX_WIDTH, mx: 'auto', px: { xs: 2, md: 4 }, minHeight: { xs: 56, sm: 64 }, gap: { xs: 1, sm: 3 } }}
                disableGutters
            >
                <Box
                    component={RouterLink}
                    to="/"
                    aria-label={`${PRODUCT_NAME}: на головну`}
                    sx={{ display: 'flex', alignItems: 'center', gap: 1.25, color: 'text.primary', textDecoration: 'none', borderRadius: 1 }}
                >
                    <Logo />
                    <Typography variant="subtitle1" component="span" fontWeight={600} sx={{ display: { xs: 'none', sm: 'inline' }, letterSpacing: '-0.01em' }}>
                        {PRODUCT_NAME}
                    </Typography>
                </Box>

                {status === 'authenticated' && (
                    <Box component="nav" aria-label="Основна навігація" sx={{ display: 'flex', gap: 0.5 }}>
                        <Button
                            component={RouterLink}
                            to="/"
                            aria-current={inPortfolios ? 'page' : undefined}
                            size="small"
                            sx={(theme) => ({
                                px: 1.5,
                                color: inPortfolios ? theme.app.accentText : theme.palette.text.secondary,
                                background: inPortfolios ? theme.app.accentSoft : 'transparent',
                            })}
                        >
                            Портфелі
                        </Button>
                    </Box>
                )}

                <Box sx={{ flex: 1 }} />

                <ThemeToggle />

                {status === 'authenticated' && user && (
                    <>
                        <Button
                            color="inherit"
                            aria-haspopup="menu"
                            aria-controls={menuAnchor ? 'user-menu' : undefined}
                            aria-expanded={menuAnchor ? 'true' : undefined}
                            onClick={(event) => setMenuAnchor(event.currentTarget)}
                            sx={{ px: { xs: 0.5, sm: 1 }, minWidth: 0, gap: 1, textTransform: 'none' }}
                        >
                            <Avatar
                                aria-hidden
                                sx={(theme) => ({ width: 32, height: 32, fontSize: 12, fontWeight: 600, background: theme.app.accentSoft, color: theme.app.accentText })}
                            >
                                {initialsOf(user.fullName, user.email)}
                            </Avatar>
                            {/* On a phone the name is hidden from the eye but still names the button for a screen reader. */}
                            <Box
                                component="span"
                                sx={(theme) => ({
                                    fontWeight: 500,
                                    maxWidth: 180,
                                    overflow: 'hidden',
                                    textOverflow: 'ellipsis',
                                    whiteSpace: 'nowrap',
                                    [theme.breakpoints.down('sm')]: visuallyHidden,
                                })}
                            >
                                {user.fullName || user.email}
                            </Box>
                            <ExpandMore fontSize="small" sx={{ color: 'text.disabled', display: { xs: 'none', sm: 'inline-block' } }} />
                        </Button>
                        <Menu
                            id="user-menu"
                            anchorEl={menuAnchor}
                            open={Boolean(menuAnchor)}
                            onClose={() => setMenuAnchor(null)}
                            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
                        >
                            <Box sx={{ px: 1.5, py: 1 }}>
                                {user.fullName && <Typography variant="body1" fontWeight={600}>{user.fullName}</Typography>}
                                <Typography variant="body2" color="text.secondary">{user.email}</Typography>
                            </Box>
                            <Divider sx={{ my: 0.5 }} />
                            <MenuItem onClick={handleSignOut}>
                                <ListItemIcon>
                                    <Logout fontSize="small" />
                                </ListItemIcon>
                                Вийти
                            </MenuItem>
                        </Menu>
                    </>
                )}

                {status === 'anonymous' && (
                    <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button component={RouterLink} to="/login" variant="outlined" size="small">Увійти</Button>
                        <Button component={RouterLink} to="/sign-up" variant="contained" size="small" sx={{ display: { xs: 'none', sm: 'inline-flex' } }}>Реєстрація</Button>
                    </Box>
                )}
            </Toolbar>
        </AppBar>
    );
}
