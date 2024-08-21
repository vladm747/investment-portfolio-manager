import React, {useEffect, useState} from "react";
import {
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow
} from "@mui/material";
import {CreatePortfolio, DeletePortfolio, GetAllPortfoliosAsync} from "../../Services/PortfolioService";
import PortfolioDto from "../../DTO/PortfolioDto";
import {Link} from "react-router-dom";
import Button from "@mui/material/Button";
import {useCookies} from "react-cookie";
import TextField from "@mui/material/TextField";
import {CreatePortfolioDto} from "../../DTO/CreatePortfolioDto";


function PortfolioList() {
    const [cookie, etCookie] = useCookies(['accessToken'])

    const userId = "78f4683e-e9eb-4251-ab23-4fafdb70b9f0";
    const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
    const [newPortfolioName, setNewPortfolioName] = useState('');
    const [isDialogOpen, setIsDialogOpen] = useState(false);

    const fetchData = async () => {
        const response = GetAllPortfoliosAsync(cookie.accessToken, userId)
        response.then((data)=>{
            setPortfolios(data);
        }).catch((error) => {
            console.error('Portfolio List. Error fetching data', error);
            throw error;
        });
    }

    useEffect(() => {
        fetchData();
    }, []);

    function formatNumber(value: number): string {
        return value.toFixed(2);
    }

    const handleCreatePortfolio = async () => {
        try {
            const newPortfolio = {
                Name: newPortfolioName,
                UserId: userId
            };
            const createdPortfolio = await CreatePortfolio(cookie.accessToken, newPortfolio);
            fetchData();
            setNewPortfolioName('');
            setIsDialogOpen(false);
        } catch (error) {
            console.error('Error creating portfolio', error);
        }
    };

    const handleDeletePortfolio = async (portfolioId: number) => {
        try {
            await DeletePortfolio(cookie.accessToken, portfolioId);
            fetchData();
        } catch (error) {
            console.error('Error deleting portfolio', error);
        }
    };


    return (
        <React.Fragment>
            <Button variant="outlined" color="success" onClick={() => setIsDialogOpen(true)}>
                Створити портфель
            </Button>
            <Dialog open={isDialogOpen} onClose={() => setIsDialogOpen(false)}>
                <DialogTitle>Create New Portfolio</DialogTitle>
                <DialogContent>
                    <TextField
                        autoFocus
                        margin="dense"
                        label="Portfolio Name"
                        fullWidth
                        value={newPortfolioName}
                        onChange={(e) => setNewPortfolioName(e.target.value)}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setIsDialogOpen(false)} color="primary">
                        Cancel
                    </Button>
                    <Button onClick={handleCreatePortfolio} color="primary">
                        Create
                    </Button>
                </DialogActions>
            </Dialog>
            <Table size="small">
                <TableHead>
                    <TableRow>
                        <TableCell>Назва</TableCell>
                        <TableCell>Загальна середня ціна входу</TableCell>
                        <TableCell>Загальна поточна ціна</TableCell>
                        <TableCell>Прибуток</TableCell>
                        <TableCell>Прибуток%</TableCell>
                        <TableCell></TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {portfolios.map((portfolio) => {
                        const hasValidValues = portfolio.totalEntryPrice != null &&
                            portfolio.totalCurrentPrice != null &&
                            portfolio.totalGain != null &&
                            portfolio.totalGainPercentage != null;

                        return (
                            <TableRow key={portfolio.id}>
                                <TableCell>{portfolio.name}</TableCell>
                                {hasValidValues ? (
                                    <>
                                        <TableCell>{portfolio.totalEntryPrice.toFixed(2)}</TableCell>
                                        <TableCell>{portfolio.totalCurrentPrice.toFixed(2)}</TableCell>
                                        <TableCell style={{ color: portfolio.totalGain > 0 ? 'green' : 'red' }}>
                                            {portfolio.totalGain.toFixed(2)}
                                        </TableCell>
                                        <TableCell style={{ color: portfolio.totalGainPercentage > 0 ? 'green' : 'red' }}>
                                            {portfolio.totalGainPercentage.toFixed(2)}%
                                        </TableCell>
                                    </>
                                ) : (
                                    <TableCell colSpan={4}></TableCell>
                                )}
                                <TableCell>
                                    <Button
                                        color="info"
                                        variant="outlined"
                                        component={Link}
                                        to="/portfolio"
                                        state={portfolio}
                                    >
                                        Переглянути портфель
                                    </Button>
                                    <Button
                                        color="error"
                                        variant="outlined"
                                        onClick={() => handleDeletePortfolio(portfolio.id)}
                                    >
                                        Видалити
                                    </Button>
                                </TableCell>
                            </TableRow>
                        );
                    })}
                </TableBody>
            </Table>
        </React.Fragment>
    );
}

export default PortfolioList;
