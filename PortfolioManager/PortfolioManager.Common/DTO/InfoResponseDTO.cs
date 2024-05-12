using Microsoft.AspNetCore.Http;

namespace PortfolioManager.Common.DTO;

public class InfoResponseDTO
{
    public bool IsSuccess { get; set; }
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public StockDTO StockDTO { get; set; }
}