using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class UserService: IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;

    public UserService(UserManager<User> manager, IMapper mapper)
    {
        _userManager = manager;
        _mapper = mapper;
    }

    public UserDTO GetCurrentUser(ClaimsPrincipal userPrincipal)
    {
        var userId = _userManager.GetUserId(userPrincipal);
        var user = _userManager.Users.SingleOrDefault(user => user.Id == userId);
        
        if (user == null)
            throw new UserNotFoundException("User Not Found");
        
        return _mapper.Map<User, UserDTO>(user);
    }

    public User GetUserById(ClaimsPrincipal userPrincipal)
    {
        var userId = _userManager.GetUserId(userPrincipal);
        var user = _userManager.Users.SingleOrDefault(user => user.Id == userId);
        
        if (user == null)
            throw new UserNotFoundException("User Not Found");
        
        return user;
    }

    public async Task<IEnumerable<UserDTO>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return _mapper.Map<IEnumerable<UserDTO>>(users);
    }
    //TODO handle null possibility
    public string GetUserId(ClaimsPrincipal userPrincipal) =>
        _userManager.GetUserId(userPrincipal);

    public IEnumerable<string> GetUsersEmails(IEnumerable<string> ids) => 
        _userManager.Users.Where(user => ids.Contains(user.Id))
            .Select(item => item.Email).AsNoTracking().ToList();
    
    // public string GetNickName(ClaimsPrincipal userPrincipal)
    // {
    //     var userId = _userManager.GetUserId(userPrincipal) ?? throw new UnauthorizedAccessException("Cant get user");
    //     
    //     var nickName = _userManager.Users.Where(user => user.Id == userId).
    //         Select(item => item.NickName).AsNoTracking().FirstOrDefault();
    //     
    //     return nickName;
    // }
    
    public async Task<IdentityResult> DeleteAsync(ClaimsPrincipal userPrincipal, string email)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        
        if (user == null)
            throw new UserNotFoundException("User Not Found");

        if (user.Email != email)
            throw new NotAllowedException("You have no permission to delete this user");

        var result = await _userManager.DeleteAsync(user);
        return result;
    }
}