import numpy as np
import pandas as pd
import yfinance as yf
import quantstats as qs
from datetime import datetime
import logging
import traceback
from pypfopt import expected_returns
from pypfopt import risk_models
from pypfopt.efficient_frontier import EfficientFrontier
from scipy.optimize import minimize
import io
from typing import List, Dict, Optional
from pydantic import BaseModel


# Налаштування базової конфігурації логування
logging.basicConfig(
    filename='app.log',        # Ім'я файлу для запису логів
    filemode='w',              # Режим запису: 'a' для додавання, 'w' для перезапису
    format='%(asctime)s - %(levelname)s - %(message)s',  # Формат повідомлень
    level=logging.INFO        # Рівень логування
)

class total_returns_stats:
    """
    A class representing the returns of a symbol.

    Attributes:
        symbol (str): The symbol of the investment.
        returns (list): A list of returns.
        dataframe: The dataframe containing the returns data.
    """

    def __init__(self, symbol='', returns=[], base_weight = 0, dataframe=pd.DataFrame()):
        self.symbol = symbol
        self.returns = returns
        self.dataframe = dataframe
        self.base_weight = base_weight
        

def get_historical_prices(symbol: str, start_date: datetime):
    stock = yf.Ticker(symbol)
    history = stock.history(start=start_date)
    history.index = history.index.tz_localize(None)  # Ensure the index is timezone-naive

    return history['Close']

def aggregate_portfolio_growth(stocks):
    earliest_date = min(stock.EntryDate for stock in stocks).date()
    end_date = datetime.today().date()
    all_dates = pd.date_range(start=earliest_date, end=end_date)
    
    total_prices = pd.Series(0, index=all_dates, dtype=float)  # Initialize total_prices with zeros

    for stock in stocks:
        history = get_historical_prices(stock.Symbol, stock.EntryDate)
        history = history * stock.Quantity

        # Reindex to ensure continuity from the earliest date to today
        history = history.reindex(all_dates, method='ffill').fillna(0)

        total_prices = total_prices.add(history, fill_value=0)

    total_prices = total_prices.fillna(method='ffill').fillna(method='bfill')
    return total_prices.to_dict()

def markowitz_mean_variance_optimization(base_weights, portfolio_symbols):

    total_stats_list = []
    # Downloading returns from quantstats
    def download_dataframes(symbols, base_weights):
        counter = 0
        for symbol in symbols:
            current_return_qs = qs.utils.download_returns(symbol)
            current_return_qs = current_return_qs.loc['2010-07-01':datetime.today().strftime('%Y-%m-%d')]
            current_df = yf.download(symbol, start = '2010-07-01', end = datetime.today().strftime('%Y-%m-%d'))
            total_stats_list.append(total_returns_stats(symbol, current_return_qs, base_weights[counter], current_df))
            counter += 1        
            
 #   logging.info("markiwitz.py called")
  #  print("base_weights length" + str(len(base_weights)))
    
    download_dataframes(portfolio_symbols, base_weights)
 #   logging.info(f"markiwitz.py total_stats_list {total_stats_list}")
    
    # def calc_portfolio(total_returns_stats, base_weights):
        
    #     base_portfolio = pd.Series(index=total_returns_stats[0].returns.index, dtype=np.float64)

    #     for total_stat in total_returns_stats:
    #         value = float(value) + float(total_stat.returns[date] * base_weights[base_counter])
    #         print(total_stat.returns * base_weights[base_counter])
    #         base_portfolio.item = base_portfolio.add(total_stat.returns * base_weights[base_counter]) 
    #         base_counter += 1
    #     return base_portfolio

    def calc_portfolio(total_returns_stats, base_weights):
        base_portfolio = pd.Series(0, index=total_returns_stats[0].returns.index)
        
        for total_stat, weight in zip(total_returns_stats, base_weights):
            if total_stat.returns.empty:
                logging.warning(f"Returns for {total_stat.symbol} are empty, skipping")
                continue
            
            # Заповнити NaN значення нулями
            adjusted_returns = total_stat.returns.fillna(0)
            portfolio_contribution = adjusted_returns * weight
            
            base_portfolio = base_portfolio.add(portfolio_contribution, fill_value=0)
        
        return base_portfolio

    base_portfolio = calc_portfolio(total_stats_list, base_weights)
    #base_portfolio = total_stats_list[0].returns * base_weights[0] + total_stats_list[1].returns * base_weights[1] + total_stats_list[2].returns * base_weights[2] + total_stats_list[3].returns * base_weights[3]
   # print(type(base_portfolio))

  #  print("BASE PORTFOLIO ====================================")
    # for date, value in base_portfolio.items():
    #     print(f"Date: {date}, Value: {value}")
    # print("===================================================")

    

    # Downloading the S&P 500 returns to future compare
    sp500 = qs.utils.download_returns('^GSPC')
    sp500 = sp500.loc['2010-07-01':datetime.today().strftime('%Y-%m-%d')]
    sp500.index = sp500.index.tz_localize('UTC').tz_convert(None)

    if sp500.empty:
        raise ValueError("S&P 500 benchmark data is empty")
    if base_portfolio.empty:
        raise ValueError("Portfolio data is empty")
    if len(base_portfolio) < len(sp500):
        raise ValueError("Base portfolio has fewer data points than the S&P 500 benchmark")
    #print(sp500)

    #ValueError: attempt to get argmax of an empty sequence

    base_portfolio_report = qs.reports.metrics(base_portfolio, benchmark = sp500, mode="full", display=False)

    dataframes = []
    symbols_list = []
    
    for returns in total_stats_list:
        symbols_list.append(returns.symbol)
        # Використовуємо тільки стовпець 'Close' і перейменовуємо стовпець на унікальну назву
        returns.dataframe = returns.dataframe[['Close']].rename(columns={'Close': returns.symbol})
        returns.dataframe.index = returns.dataframe.index.tz_localize('UTC').tz_convert(None)
        dataframes.append(returns.dataframe)

   # print(f"Dataframes length:  {len(dataframes)} {type(dataframes)}")
    common_index = dataframes[0].index
    
    # Звірка індексів
    common_index = dataframes[0].index
    for df in dataframes[1:]:
        common_index = common_index.intersection(df.index)

    for i in range(len(dataframes)):
        dataframes[i] = dataframes[i].reindex(common_index)

    merged_df = pd.concat(dataframes, join='outer', axis=1)

   # print(f"merged_df shape: {merged_df.shape}")
   # print(f"symbols_list length: {len(symbols_list)}")

    # Перевірка кількості стовпців і символів
    if merged_df.shape[1] != len(symbols_list):
        logging.error(f"Length mismatch: merged_df has {merged_df.shape[1]} columns, but symbols_list has {len(symbols_list)} elements.")
        raise ValueError(f"Length mismatch: merged_df has {merged_df.shape[1]} columns, but symbols_list has {len(symbols_list)} elements.")



    # Calculating the annualized expected returns and the annualized sample covariance matrix
    mu = expected_returns.mean_historical_return(merged_df)

    #expected returns
    S = risk_models.sample_cov(merged_df) #Covariance matrix

    # Optimizing for maximal Sharpe ratio
    ef = EfficientFrontier(mu, S) # Providing expected returns and covariance matrix as input

    weights = ef.max_sharpe() # Optimizing weights for Sharpe ratio maximization 

#clean_weights = ef.clean_weights() # clean_weights rounds the weights and clips near-zeros

    clean_indexes = []

    # for i in range(len(clean_weights)):
    #     if list(merged_df.columns)[i] == 0:
    #         clean_weights.remove(i)  # Видалення ваги 0 з списку
    #     clean_indexes.append(i)
    #     print(f'{list(merged_df.columns)[i]}: {clean_weights[list(merged_df.columns)[i]]}')

    # if not clean_weights:
    #     raise ValueError("clean_weights is empty, optimization might have failed")
    # print(f"clean_weights: {clean_weights}")

    # for symbol, weight in clean_weights.items():
    #     print(f'{symbol}: {weight}')

    optimized_portfolio = calc_portfolio(total_stats_list, weights.values())
    

    # optimized_portfolio = pd.Series(index=base_portfolio.index, dtype=float)
    
    # index_count = 0
    # for total_stats in total_stats_list:
    #     optimized_portfolio = optimized_portfolio.add(total_stats.returns * clean_weights[index_count], fill_value=0)
    #     index_count += 1

   


     # Збереження метрик у змінну
    optimized_portfolio_report = qs.reports.metrics(optimized_portfolio, benchmark=base_portfolio, mode="full", display=False)

    weights = {
        "Weights":{
            "BaseWeights": base_weights,
            "OptimizedWeights": weights
        }
        
    }

    return base_portfolio_report, optimized_portfolio_report, weights

class StockDTO(BaseModel):
    id: int
    symbol: str
    name: str
    quantity: int
    entry_price: float
    current_price: float
    total_value: float
    gain: float
    gain_percentage: float
    entry_date: datetime
    currency: str
    sector: str
    portfolio_id: int


stock1 = StockDTO(
    id=1,
    symbol='MSFT',
    name='Microsoft Corporation',
    quantity=2,
    entry_price=100,
    current_price=150,
    total_value=300,
    gain=50,
    gain_percentage=50,
    entry_date=datetime(2021, 1, 1),
    currency='USD',
    sector='Technology',
    portfolio_id=1
)

stock2 = StockDTO(
    id=2,
    symbol='AAPL',
    name='Apple Inc',
    quantity=3,
    entry_price=120,
    current_price=130,
    total_value=390,
    gain=10,
    gain_percentage=8.33,
    entry_date=datetime(2022, 2, 1),
    currency='USD',
    sector='Technology',
    portfolio_id=1
)

stocks = [stock1, stock2]

try:
    portfolio_growth = aggregate_portfolio_growth(stocks)
    print(portfolio_growth)
except Exception as e:
    logging.error("An error occurred during the optimization process", exc_info=True)
    print(f"Exception: {e}")
    print(f"Traceback: {traceback.format_exc()}")