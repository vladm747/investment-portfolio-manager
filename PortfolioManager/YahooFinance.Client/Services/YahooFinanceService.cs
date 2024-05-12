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
        var client = new RestClient("https://yahoo-finance-data2.p.rapidapi.com/price?start=1&limit=100");
        var request = new RestRequest("", Method.Post);
        request.AddHeader("content-type", "application/json");
        request.AddHeader("X-RapidAPI-Key", "1c78334ab9mshac46f7830b41f62p1bd339jsn9173bb9244a2");
        request.AddHeader("X-RapidAPI-Host", "yahoo-finance-data2.p.rapidapi.com");
        request.AddParameter("application/json", "{\r \"symbol\": \"" + symbol +  "\",\r \"key\": \"1d\"\r }",
            ParameterType.RequestBody);
        var response = client.Execute<string>(request);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var priceObject = JsonConvert.DeserializeObject<PriceJsonObject.Root>(response.Content);
            return new PriceResponseDTO()
            {
                IsSuccess = true,
                CurrentPrice = priceObject?.Data.result[0]?.close
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
        
        var client = new RestClient("https://yahoo-finance-data2.p.rapidapi.com/detail-info");
        var request = new RestRequest("", Method.Post);
        
        request.AddHeader("content-type", "application/json");
        request.AddHeader("X-RapidAPI-Key", "1c78334ab9mshac46f7830b41f62p1bd339jsn9173bb9244a2");
        request.AddHeader("X-RapidAPI-Host", "yahoo-finance-data2.p.rapidapi.com");
        request.AddParameter("application/json", "{\r\"symbol\":" + symbol + "\r }", ParameterType.RequestBody);
        
        var response = client.Execute<string>(request);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var stock = JsonConvert.DeserializeObject<StockJsonObject.Root>(response.Content);
            
            stockInfoResponse.StockDTO.Symbol = symbol;
            stockInfoResponse.StockDTO.Name = stock.Data.shortName;
            stockInfoResponse.StockDTO.CurrentPrice = stock.Data.currentPrice;
            stockInfoResponse.StockDTO.Currency = stock.Data.currency;
            
            var sector = stock.Data.sector.Trim();
            
            stockInfoResponse.StockDTO.Sector = Enum.Parse<SectorEnum>(sector);
            stockInfoResponse.StockDTO.EntryDate = DateTime.Now;
            stockInfoResponse.StockDTO.EntryPrice = stock.Data.currentPrice ?? 0;
            
            stockInfoResponse.IsSuccess = true;

            return stockInfoResponse;
        }
        else
        {
            stockInfoResponse.IsSuccess = false;
            stockInfoResponse.Code = (int)response.StatusCode;
            stockInfoResponse.Message = response.ErrorMessage ?? "Error while getting stock info";
            
            return stockInfoResponse;                
        }
    }
}