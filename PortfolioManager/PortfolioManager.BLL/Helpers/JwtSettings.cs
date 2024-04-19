namespace PortfolioManager.BLL.Helpers;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int ExpirationInHours { get; set; }
}