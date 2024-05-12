using AutoMapper;
using PortfolioManager.Common.DTO;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Profiles;

public class AutoMapperProfiles: Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<User, UserDTO>().ReverseMap();
        CreateMap<Stock, StockDTO>().ReverseMap();
        CreateMap<Portfolio, PortfolioDTO>().ReverseMap();
    }
}