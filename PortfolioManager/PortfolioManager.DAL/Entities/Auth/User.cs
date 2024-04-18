using Microsoft.AspNetCore.Identity;

namespace PortfolioManager.DAL.Entities.Auth;

public class User: IdentityUser
{
    public override string Id { get; set; }
}