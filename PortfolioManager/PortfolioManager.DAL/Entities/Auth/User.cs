using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PortfolioManager.DAL.Entities.Auth;

public class User: IdentityUser
{
    public override string Id { get; set; }
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenCreated { get; set; }
    public DateTime TokenExpires { get; set; }
    
    public ICollection<Portfolio>? Portfolios { get; set; }
}