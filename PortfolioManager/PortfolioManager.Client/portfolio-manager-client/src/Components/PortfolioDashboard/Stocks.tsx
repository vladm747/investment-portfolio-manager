import React, {useEffect, useState} from "react";
import {Title} from "@mui/icons-material";
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
import {CreateStock, DeleteStock, GetAllStocksAsync, GetStockAsync, UpdateStockInfo} from "../../Services/StockService";
import {getSectorText, SectorEnum} from "../../DTO/StockDto";
import StockDto from "../../DTO/StockDto";
import Button from "@mui/material/Button";
import {useCookies} from "react-cookie";
import TextField from "@mui/material/TextField";
import CreateStockDto from "../../DTO/CreateStockDto";

type StocksTableProps = {
    portfolioId: number;
};

const StocksTable: React.FC<StocksTableProps> = ({ portfolioId }) => {
    const [stocks, setStocks] = useState<StockDto[]>([]);
    const [cookie] = useCookies(['accessToken']);
    const [newStock, setNewStock] = useState({ symbol: '', quantity: 0, entryPrice: 0, entryDate: new Date() });
    const [isDialogOpen, setIsDialogOpen] = useState(false);

    const fetchData = async () => {
        const response = GetAllStocksAsync(cookie.accessToken, portfolioId)
        response.then((data)=>{
            setStocks(data);
        }).catch((error) => {
            console.error('Stock list. Error fetching data', error);
            throw error;
        });
    };

    const handleCreateStock = async () => {
        try {
            const newStockDto: CreateStockDto = {
                portfolioId,
                quantity: newStock.quantity,
                symbol: newStock.symbol,
                entryPrice: newStock.entryPrice,
                entryDate: newStock.entryDate
            };
            await CreateStock(cookie.accessToken, newStockDto);
            await fetchData();
            setNewStock({ symbol: '', quantity: 0, entryPrice: 0, entryDate: new Date() });
            setIsDialogOpen(false);
        } catch (error) {
            console.error('Error creating stock', error);
        }
    };

    const handleDeleteStock = (stockId: number) => async() => {
        try {
            await DeleteStock(cookie.accessToken, stockId);
            await fetchData();
        } catch (error) {
            console.error('Error deleting stock', error);
        }
    };

    useEffect(() => {
        fetchData();
    }, []);

    const groupedStocks = stocks.reduce((groups, stock) => {
        const sector = stock.sector;
        if (!groups[sector]) {
            groups[sector] = [];
        }
        groups[sector].push(stock);
        return groups;
    }, {} as Record<string, StockDto[]>);

    function formatNumber(value: number): string {
        return value.toFixed(2);
    }
    const handleStockUpdateClick = async () =>{
        await UpdateStockInfo(cookie.accessToken, portfolioId)
        await fetchData();
    }
    return (
        <React.Fragment>
            <Dialog open={isDialogOpen} onClose={() => setIsDialogOpen(false)}>
                <DialogTitle>Додати Акцію</DialogTitle>
                <DialogContent>
                    <TextField
                        autoFocus
                        margin="dense"
                        label="Тікер"
                        fullWidth
                        value={newStock.symbol}
                        onChange={(e) => setNewStock({ ...newStock, symbol: e.target.value })}
                    />
                    <TextField
                        margin="dense"
                        label="Кількість"
                        fullWidth
                        type="number"
                        value={newStock.quantity}
                        onChange={(e) => setNewStock({ ...newStock, quantity: Number(e.target.value) })}
                    />
                    <TextField
                        margin="dense"
                        label="Середня ціна входу"
                        fullWidth
                        type="number"
                        value={newStock.entryPrice}
                        onChange={(e) => setNewStock({ ...newStock, entryPrice: Number(e.target.value) })}
                    />
                    <TextField
                        margin="dense"
                        label="Дата входу"
                        fullWidth
                        type="date"
                        value={newStock.entryDate.toISOString().substring(0, 10)}
                        onChange={(e) => setNewStock({ ...newStock, entryDate: new Date(e.target.value) })}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setIsDialogOpen(false)} color="primary">Cancel</Button>
                    <Button onClick={handleCreateStock} color="primary">Create</Button>
                </DialogActions>
            </Dialog>

            <Title>Акції</Title>
            <Table size="small">
                <TableHead>
                    <TableRow>
                        <Button color="success" variant="outlined" onClick={handleStockUpdateClick}>Оновити ціни</Button>
                    </TableRow>
                    <TableRow>
                        <TableCell>Сектор</TableCell>
                        <TableCell>Тікер</TableCell>
                        <TableCell>Кількість</TableCell>
                        <TableCell>Ціна входу</TableCell>
                        <TableCell>Поточна ціна</TableCell>
                        <TableCell>Загальна ціна</TableCell>
                        <TableCell>Прибуток</TableCell>
                        <TableCell>Прибуток%</TableCell>
                        <TableCell>Дата входу</TableCell>
                        <TableCell>
                            <Button color="success" variant="outlined" onClick={() => setIsDialogOpen(true)}>Додати Акцію</Button>
                        </TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {Object.entries(groupedStocks).map(([sector, stocks]) =>
                        stocks.map((stock, index) => (
                            <TableRow>
                                {index === 0 && <TableCell rowSpan={stocks.length}>{getSectorText(Number(sector))}</TableCell>}
                                <TableCell>{stock.symbol}</TableCell>
                                <TableCell>{stock.quantity}</TableCell>
                                <TableCell>{formatNumber(stock.entryPrice) + " " + stock.currency}</TableCell>
                                <TableCell style={{color: stock.entryPrice < stock.currentPrice ? "green" : "red" }}>{formatNumber(stock.currentPrice) + " " + stock.currency}</TableCell>
                                <TableCell>{formatNumber(stock.totalCurrentPrice)}</TableCell>
                                <TableCell style={{color: stock.gain > 0 ? "green" : "red"}}>{formatNumber(stock.gain) + " " + stock.currency}</TableCell>
                                <TableCell style={{color: stock.gain > 0 ? "green" : "red"}}>{formatNumber(stock.gainPercentage) + "%"}</TableCell>
                                <TableCell>{new Date(stock.entryDate).toLocaleDateString()}</TableCell>
                                <TableCell>
                                    <Button color="error" variant="outlined" onClick={handleDeleteStock(stock.id ?? 0)} >Видалити</Button>
                                </TableCell>
                            </TableRow>
                        ))
                    )}
                </TableBody>
            </Table>
        </React.Fragment>
    )
}

export default StocksTable;
