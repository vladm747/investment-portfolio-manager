using PortfolioManager.Common.DTO;

namespace YahooFinance.Client.Interfaces;

public interface IYahooFinanceService
{
    public Task<InfoResponseDTO> GetInfoAsync(string symbol);
    public PriceResponseDTO GetPriceAsync(string symbol);
}