"""Risk forecast for the next three months (63 trading days) with the trained model risk-v1.

    features.py   the model's features, the same module as in training
    garch.py      GARCH(1,1), averaged with the model's volatility forecast
    model.py      loading the model files and forecasting a frame of price series
    portfolio.py  calendar, portfolio series, forecasts of the portfolio and its stocks
"""
