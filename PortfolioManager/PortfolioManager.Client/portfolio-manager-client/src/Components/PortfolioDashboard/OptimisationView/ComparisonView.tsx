import React, {useEffect, useState} from 'react';
import { Container, Typography, Table, TableBody, TableCell, CircularProgress, TableContainer, TableHead, TableRow, Paper } from '@mui/material';
import {useCookies} from "react-cookie";
import {GetOptimisationData} from "../../../Services/StatisticService";
import {Link, useParams} from "react-router-dom";
import Button from "@mui/material/Button";
import Grid from "@mui/material/Grid";

interface ComparisonViewModel {
    metricName: string;
    strategyBaseValue: number;
    strategyOptimizedValue: number;
}

interface Weights {
    baseWeights: number[];
    optimizedWeights: { [key: string]: number };
}

interface ComparisonResult {
    comparisons: ComparisonViewModel[];
    weights: Weights;
}


const ComparisonView: React.FC = () => {
    const { portfolioId } = useParams<{ portfolioId: string }>();
    const [cookie] = useCookies(['accessToken']);
    const [data, setData] = useState<ComparisonResult >();
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const fetchedData = await GetOptimisationData(cookie.accessToken, Number(portfolioId));
                setData(fetchedData);
                setLoading(false);
            } catch (error) {
                console.error('ComparisonView. Error fetching data', error);
                setLoading(false);
            }
        };
        fetchData();
    }, [cookie.accessToken, portfolioId]);

    const getColor = (baseValue: number, optimizedValue: number, metricName: string) => {
        if (metricName === 'Максимальна просадка') {
            return baseValue < optimizedValue ? 'red' : 'green';
        }
        if (metricName === 'Показник волатильності (річний)' || baseValue == optimizedValue) {
            return 'black';
        }

        return baseValue > optimizedValue ? 'green' : 'red';
    };

    if (loading) {
        return (
            <Container>
                <CircularProgress />
            </Container>
        );
    }

    if (!data) {
        return (
            <Container>
                <Typography variant="h6" component="h2">
                    Немає даних для відображення.
                </Typography>
            </Container>
        );
    }
    const optimizedWeightKeys = Object.keys(data!.weights.optimizedWeights);
    const baseWeights = data!.weights.baseWeights;

    function formatNumber(value: number): string {
        return value.toFixed(4);
    }

    return (
        <Container>
            <Grid item xs={12}>
                <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
                    <Button
                        onClick={() => window.history.back()}
                        color="info"
                        variant="outlined"
                    >
                        Повернутись до огляду портфеля
                    </Button>
                </Paper>
            </Grid>

            <Typography variant="h4" component="h1" gutterBottom>
                Порівняння портфельних стратегій
            </Typography>

            <Typography variant="h6" component="h2" gutterBottom>
                Таблиця ваг акцій у портфелі
            </Typography>
            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>Класифікація вагів</TableCell>
                            {optimizedWeightKeys.map((symbol, index) => (
                                <TableCell key={index}>{symbol}</TableCell>
                            ))}
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        <TableRow>
                            <TableCell>Базовий портфель</TableCell>
                            {optimizedWeightKeys.map((symbol, index) => (
                                <TableCell key={index}>{formatNumber(baseWeights[index])}</TableCell>
                            ))}
                        </TableRow>
                        <TableRow>
                            <TableCell>Оптимізований портфель</TableCell>
                            {optimizedWeightKeys.map((symbol) => (
                                <TableCell key={symbol}>{formatNumber(data!.weights.optimizedWeights[symbol])}</TableCell>
                            ))}
                        </TableRow>
                    </TableBody>
                </Table>
            </TableContainer>

            <Typography variant="h6" component="h2" gutterBottom>
                Порівняння стратегій
            </Typography>
            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>Назва критерію</TableCell>
                            <TableCell>Базовий портфель</TableCell>
                            <TableCell>Оптимізований портфель</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {data!.comparisons.map((comparison, index) => (
                            <TableRow key={index}>
                                <TableCell>{comparison.metricName}</TableCell>
                                <TableCell style={{ color: getColor(comparison.strategyBaseValue, comparison.strategyOptimizedValue, comparison.metricName) }}>
                                    {comparison.strategyBaseValue}
                                </TableCell>
                                <TableCell style={{ color: getColor(comparison.strategyOptimizedValue, comparison.strategyBaseValue, comparison.metricName) }}>
                                    {comparison.strategyOptimizedValue}
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </Container>
    );
};

export default ComparisonView;
