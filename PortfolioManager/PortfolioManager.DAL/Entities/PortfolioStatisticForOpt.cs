using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.DAL.Entities;

public class PortfolioStatisticForOpt
{
    [Key]
    public int Id { get; set; }
    public string Symbols { get; set; }
    public string Weights { get; set; }
    public DateTime LastUpdate { get; set; }
    public int PortfolioId { get; set; }
}