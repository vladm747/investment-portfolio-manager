import React, {useEffect, useState} from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import PortfolioDto from '../../DTO/PortfolioDto';
import {GetGrowthStatsData, GetOptimisationData} from "../../Services/StatisticService";
import {useCookies} from "react-cookie";
import Container from "@mui/material/Container";
import {CircularProgress} from "@mui/material";
import Typography from "@mui/material/Typography"; // Adjust the import path as necessary
import { Brush } from 'recharts';

type PortfolioChartProps = {
    portfolioId: number;
};

type GrowthData = {
    [key: string]: number;
};

const PortfolioChart: React.FC<PortfolioChartProps> = ({ portfolioId }) => {
    const [cookie] = useCookies(['accessToken']);
    const [data, setData]
        = useState<Array<{ date: string; value: number }>>([]);
    const [loading, setLoading] = useState<boolean>(true);


    useEffect(() => {
        const fetchData = async () => {
            try {
                const fetchedData = await GetGrowthStatsData(cookie.accessToken, Number(portfolioId));
                if (fetchedData && fetchedData.growthData) {
                    const formattedData = Object.entries(fetchedData.growthData).map(([date, value]) => ({
                        date: new Date(date).toLocaleDateString(),
                        value: value as number,
                    }));
                    setData(formattedData);
                }
                setLoading(false);
            } catch (error) {
                console.error('Error fetching data', error);
                setLoading(false);
            }
        };
        fetchData();
    }, [cookie.accessToken, portfolioId]);

    if (loading) {
        return (
            <Container>
                <CircularProgress />
            </Container>
        );
    }

    if (!data.length) {
        return (
            <Container>
                <Typography variant="h6" component="h2">
                    Немає даних для відображення.
                </Typography>
            </Container>
        );
    }

    return (
        <ResponsiveContainer width="100%" height={400}>
            <LineChart
                data={data}
                margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
            >
                <Brush dataKey="date" height={30} stroke="#8884d8" />
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip />
                <Line type="monotone" dataKey="value" stroke="#8884d8" />
            </LineChart>
        </ResponsiveContainer>
    );
};

    // return (
    //     <ResponsiveContainer width="100%" height={400}>
    //         <LineChart
    //             data={data}
    //             margin={{
    //                 top: 20,
    //                 right: 30,
    //                 left: 20,
    //                 bottom: 5,
    //             }}
    //         >
    //             <CartesianGrid strokeDasharray="3 3" />
    //             <XAxis dataKey="name" />
    //             <YAxis />
    //             <Tooltip />
    //             <Legend />
    //             <Line type="monotone" dataKey="currentPrice" stroke="#8884d8" />
    //             <Line type="monotone" dataKey="entryPrice" stroke="#82ca9d" />
    //             <Line type="monotone" dataKey="totalValue" stroke="#ffc658" />
    //             <Line type="monotone" dataKey="gain" stroke="#ff7300" />
    //         </LineChart>
    //     </ResponsiveContainer>
    // );
// };

export default PortfolioChart;
