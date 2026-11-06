import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import DarkModeOutlined from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlined from '@mui/icons-material/LightModeOutlined';
import { useThemeMode } from './ThemeModeProvider';

/** Switches between the light and the dark theme. The label says what a click will do. */
export default function ThemeToggle() {
    const { mode, toggleMode } = useThemeMode();
    const label = mode === 'dark' ? 'Увімкнути світлу тему' : 'Увімкнути темну тему';

    return (
        <Tooltip title={label}>
            <IconButton
                aria-label={label}
                onClick={toggleMode}
                sx={{ width: 40, height: 40, border: 1, borderColor: 'divider', color: 'text.secondary' }}
            >
                {mode === 'dark' ? <LightModeOutlined fontSize="small" /> : <DarkModeOutlined fontSize="small" />}
            </IconButton>
        </Tooltip>
    );
}
