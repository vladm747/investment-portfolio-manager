using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface IUserService
{
    UserDTO GetCurrentUser(ClaimsPrincipal userPrincipal);
    User GetUserById(ClaimsPrincipal userPrincipal); 
    Task<IEnumerable<UserDTO>> GetAllAsync();
    string GetUserId(ClaimsPrincipal userPrincipal);
    IEnumerable<string> GetUsersEmails(IEnumerable<string> ids);
    //string GetNickName(ClaimsPrincipal userPrincipal);
    Task<IdentityResult> DeleteAsync(ClaimsPrincipal user, string email);
}