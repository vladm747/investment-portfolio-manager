namespace PortfolioManager.Common.DTO;

public class PriceResponseDTO
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public double CurrentPrice { get; set; }
    public bool IsSuccess { get; set; }
}