from fastapi import FastAPI, Body, HTTPException
from Markowitz import markowitz_mean_variance_optimization
import uvicorn
app = FastAPI()
import logging
import traceback
# Налаштування базової конфігурації логування
logging.basicConfig(
    filename='app.log',        # Ім'я файлу для запису логів
    filemode='w',              # Режим запису: 'a' для додавання, 'w' для перезапису
    format='%(asctime)s - %(levelname)s - %(message)s',  # Формат повідомлень
    level=logging.INFO        # Рівень логування
)


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
    

@app.get('/api/math/get-message')
def get_message():
    return {'message': 'Hello from the math controller!'}

if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8011, reload=False, log_level="debug",
                workers=1, limit_concurrency=1, limit_max_requests=1)