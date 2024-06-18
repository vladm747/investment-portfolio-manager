from fastapi import FastAPI, Body, HTTPException
from Markowitz import markowitz_mean_variance_optimization, aggregate_portfolio_growth
import uvicorn
import yfinance as yf
from datetime import datetime
from pydantic import BaseModel
app = FastAPI()     
import logging
from typing import List

import traceback
# Налаштування базової конфігурації логування
logging.basicConfig(
    filename='app.log',        # Ім'я файлу для запису логів
    filemode='w',              # Режим запису: 'a' для додавання, 'w' для перезапису
    format='%(asctime)s - %(levelname)s - %(message)s',  # Формат повідомлень
    level=logging.INFO        # Рівень логування
)

class StockDTO(BaseModel):
    Id: int
    Symbol: str
    Name: str
    Quantity: int
    EntryPrice: float
    CurrentPrice: float
    TotalCurrentPrice: float
    Gain: float
    GainPercentage: float
    EntryDate: datetime
    Currency: str
    Sector: int
    PortfolioId: int


@app.post("/api/portfolio-growth")
async def get_portfolio_growth(stocks: List[StockDTO]):
    try:
        growth_data = aggregate_portfolio_growth(stocks)
        return growth_data
    except Exception as e:
        logging.error(f"An error occurred: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Internal Server Error error: {str(e)}.\n Exception arguments: {e.args} \n Exception detailed message: {repr(e)} <br> Traceback: {traceback.format_exc()}")



@app.get("/")
def read_root():
    return {"Hello": "World"}

@app.post("/api/get-portfolio-opt")
def get_portfolio_opt(data: dict = Body(...)):
    try:
        logging.info("get_portfolio_opt called")

        base_weights = data['base_weights']
        portfolio_symbols = data['portfolio_symbols']
        logging.info(f"base_weights: {base_weights}, portfolio_symbols: {portfolio_symbols}")

        result = markowitz_mean_variance_optimization(base_weights, portfolio_symbols)
        return result
    except Exception as e:
            logging.error(f"An error occurred: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Internal Server Error error: {str(e)}.\n Exception arguments: {e.args} \n Exception detailed message: {repr(e)} <br> Traceback: {traceback.format_exc()}") 
    
@app.get("/yfinance/{symbol}")
async def get_stock_info(symbol: str):
    try:
        stock = yf.Ticker(symbol)
        stock_info = stock.info
        return stock_info
    except Exception as e:
        raise HTTPException(status_code=404, detail="Stock symbol not found")

@app.get("/yfinance/{symbol}/price")
async def get_stock_price(symbol: str):
    try:
        stock = yf.Ticker(symbol)
        stock_price = stock.history(period="1d")["Close"].iloc[-1]
        return {"symbol": symbol, "price": stock_price}
    except Exception as e:
        raise HTTPException(status_code=404, detail="Stock symbol not found")

if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8011, reload=False, log_level="debug",
                workers=1, limit_concurrency=1, limit_max_requests=1)