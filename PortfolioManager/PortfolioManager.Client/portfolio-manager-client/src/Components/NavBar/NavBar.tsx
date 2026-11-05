import AppBar from '@mui/material/AppBar';
import Box from '@mui/material/Box';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import ListItemIcon from '@mui/material/ListItemIcon';
import Divider from '@mui/material/Divider';
import MenuIcon from '@mui/icons-material/Menu';
import AccountCircle from '@mui/icons-material/AccountCircle';
import Logout from '@mui/icons-material/Logout';
import {useState} from "react";
import { Link, useNavigate } from 'react-router-dom';
import {useAuth} from "../../Auth/AuthProvider";

function NavBar() {
    const { status, user, signOut } = useAuth();
    const navigate = useNavigate();
    const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);

    const handleSignOut = () => {
        setMenuAnchor(null);
        signOut();
        navigate('/login', { replace: true });
    };

  return (
      <Box sx={{ flexGrow: 1 }}>
          <AppBar position="static">
              <Toolbar>
                  <IconButton
                      size="large"
                      edge="start"
                      color="inherit"
                      aria-label="menu"
                      sx={{ mr: 2 }}
                  >
                      <MenuIcon />
                  </IconButton>
                  <Typography
                      variant="h6"
                      component={Link}
                      to="/"
                      sx={{ flexGrow: 1, color: 'inherit', textDecoration: 'none' }}
                  >
                      Portfolio Manager
                  </Typography>

                  {status === 'authenticated' && user && (
                      <>
                          <Button
                              color="inherit"
                              startIcon={<AccountCircle />}
                              aria-haspopup="true"
                              aria-controls={menuAnchor ? 'user-menu' : undefined}
                              aria-expanded={menuAnchor ? 'true' : undefined}
                              onClick={(event) => setMenuAnchor(event.currentTarget)}
                              sx={{ textTransform: 'none' }}
                          >
                              {user.fullName || user.email}
                          </Button>
                          <Menu
                              id="user-menu"
                              anchorEl={menuAnchor}
                              open={Boolean(menuAnchor)}
                              onClose={() => setMenuAnchor(null)}
                              anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                              transformOrigin={{ vertical: 'top', horizontal: 'right' }}
                          >
                              <MenuItem disabled>{user.email}</MenuItem>
                              <Divider />
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
                      <>
                          <Button color="inherit" component={Link} to="/login">Увійти</Button>
                          <Button color="inherit" component={Link} to="/sign-up">Реєстрація</Button>
                      </>
                  )}
              </Toolbar>
          </AppBar>
      </Box>
  );
}

export default NavBar;
