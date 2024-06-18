using Newtonsoft.Json;
using PortfolioManager.Common;
using PortfolioManager.Common.DTO;
using RestSharp;
using YahooFinance.Client.Interfaces;
using YahooFinance.Client.Models;

namespace YahooFinance.Client.Services;

public class YahooFinanceService: IYahooFinanceService
{
    public PriceResponseDTO GetPriceAsync(string symbol)
    {
        var options = new RestClientOptions("http://localhost:8011")
        {
            MaxTimeout = -1,
        };
        var client = new RestClient(options);
        var request = new RestRequest($"/yfinance/{symbol}/price", Method.Get);
        request.AddHeader("Content-Type", "application/json");
        RestResponse response = client.Execute<string>(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var priceObject = JsonConvert.DeserializeObject<PriceJsonObject>(response.Content);
            return new PriceResponseDTO()
            {
                IsSuccess = true,
                CurrentPrice = priceObject?.price ?? 0,
            };
        }
        else
        {
            return new PriceResponseDTO()
            {
                IsSuccess = false,
                Message = response.ErrorMessage ?? "Error while getting stock price",
                Code = (int)response.StatusCode,
            };        
        }
    }

    
    public async Task<InfoResponseDTO> GetInfoAsync(string symbol)
    {
        InfoResponseDTO stockInfoResponse = new InfoResponseDTO();
        
        var options = new RestClientOptions("http://localhost:8011")
        {
            MaxTimeout = -1,
        };
        var client = new RestClient(options);
        var request = new RestRequest($"/yfinance/{symbol}", Method.Get);
        request.AddHeader("Content-Type", "application/json");
        RestResponse response = await client.ExecuteAsync(request);
        if (response.IsSuccessful)
        {
            // Парсинг JSON відповіді у модель StockInfo
            if (response.Content != null)
            {
                StockInfoJsonObject stockInfo = JsonConvert.DeserializeObject<StockInfoJsonObject>(response.Content);
        
                stockInfoResponse.StockDTO = new StockDTO();
                stockInfoResponse.StockDTO.Symbol = symbol;
                stockInfoResponse.StockDTO.Name = stockInfo!.ShortName;
                stockInfoResponse.StockDTO.CurrentPrice = stockInfo.CurrentPrice;
                stockInfoResponse.StockDTO.Currency = stockInfo.Currency;
            
                var sector = stockInfo.Sector.Trim();
            
                stockInfoResponse.StockDTO.Sector = Enum.Parse<SectorEnum>(sector);
            }

            stockInfoResponse.IsSuccess = true;

            return stockInfoResponse;
        }
        else
        {
            stockInfoResponse.IsSuccess = false;
            stockInfoResponse.Code = (int)response.StatusCode;
            stockInfoResponse.Message = response.ErrorMessage ?? "Error while getting stockInfo info";
            
            return stockInfoResponse;                
        }
    }
}