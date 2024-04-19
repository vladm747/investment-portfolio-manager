using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<string> SignUp(SignUpDTO model);
    Task<User> SignIn(SignInDTO model);
    Task SignOut();
}