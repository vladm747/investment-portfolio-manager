import Alert from '@mui/material/Alert';
import AlertTitle from '@mui/material/AlertTitle';
import Box from '@mui/material/Box';
import RiskForecastDto from '../../DTO/RiskForecastDto';
import { riskNotices } from '../../Domain/risk';

/** What to know before trusting the figures: an old model, an unusual portfolio or stock, a stock without a forecast. */
export default function RiskNotices({ forecast }: { forecast: Pick<RiskForecastDto, 'model' | 'portfolio' | 'assets'> }) {
    const notices = riskNotices(forecast);
    if (notices.length === 0) {
        return null;
    }
    return (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
            {notices.map((notice) => (
                <Alert key={notice.title} severity={notice.severity} sx={{ alignItems: 'flex-start' }}>
                    <AlertTitle sx={{ fontWeight: 600 }}>{notice.title}</AlertTitle>
                    {notice.text}
                </Alert>
            ))}
        </Box>
    );
}
