import {createTheme, ThemeProvider} from "@mui/material/styles";
import React from "react";
import CssBaseline from "@mui/material/CssBaseline";
import Paper from "@mui/material/Paper";
import Container from "@mui/material/Container";
import Box from "@mui/material/Box";
import Grid from "@mui/material/Grid";
import StocksTable from "./Stocks";
import PortfolioDto from "../../DTO/PortfolioDto";
import {Link, useLocation} from "react-router-dom";
import Typography from "@mui/material/Typography";
import PortfolioChart from "./PortfolioChart";
import Button from "@mui/material/Button";
import { useNavigate } from "react-router-dom";


const PortfolioDashboard = () => {
    const navigate = useNavigate();

    const defaultTheme = createTheme();
    const location = useLocation();
    const portfolio  = location.state as PortfolioDto;

    const handleNavigateToOpt = () => {
        navigate(`/comparison/${portfolio.id}`);
    };
    const handleNavigatePortfolioList = () => {
        navigate(`/`);
    };

    function formatNumber(value: number): string {
        return value.toFixed(2);
    }

    return (
        <ThemeProvider theme={defaultTheme}>
            <Button
                onClick={handleNavigatePortfolioList}
                color="info"
                variant="outlined"
            >
                Back To Portfolio Dashboard
            </Button>
            <Box sx={{ display: 'flex' }}>
                <CssBaseline />
                <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
                    <Grid container spacing={3}>
                        {/* Chart */}
                        <Grid item xs={12} md={8} lg={9}>
                            <Paper
                                sx={{
                                    p: 2,
                                    display: 'flex',
                                    flexDirection: 'column',
                                    height: 430,
                                }}
                            >
                                {<PortfolioChart portfolioId={portfolio.id}/>}
                            </Paper>
                        </Grid>
                        {/* Recent Deposits */}
                        <Grid item xs={12} md={4} lg={3}>
                            <Paper
                                sx={{
                                    p: 2,
                                    display: 'flex',
                                    flexDirection: 'column',
                                    height: 240,
                                }}
                            >
                                <Typography variant="h6">  Name: {portfolio.name}</Typography>
                                <Typography>Total Value: {formatNumber(portfolio.totalCurrentPrice)} USD</Typography>
                                Total Gain: <Typography style={{color: portfolio.totalGain > 0? "green" : "red" }}> {formatNumber(portfolio.totalGain) } USD ({formatNumber(portfolio.totalGainPercentage)}% )</Typography>
                                <Button
                                    color="success"
                                    variant="outlined"
                                    onClick={handleNavigateToOpt}
                                >
                                    Подивитись порівняння оптиміації
                                </Button>
                            </Paper>
                        </Grid>
                        <Grid item xs={12}>
                            <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
                                {<StocksTable portfolioId={portfolio.id} />}
                            </Paper>
                        </Grid>
                    </Grid>
                </Container>
            </Box>
    </ThemeProvider>
    )
}

export default PortfolioDashboard;
