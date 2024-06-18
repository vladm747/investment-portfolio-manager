using AutoMapper;
using PortfolioManager.BLL.Models;
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
        CreateMap<Portfolio, CreatePortfolioDTO>().ReverseMap();
        CreateMap<Portfolio, PortfolioDTO>().ReverseMap();
        CreateMap<MetricsDTO, Metrics>()
            .ForMember(dest => dest.SmartSharpe, opt => opt.Ignore())
            .ForMember(dest => dest.SmartSortino, opt => opt.Ignore())
            .ForMember(dest => dest.SortinoSqrt2, opt => opt.Ignore())
            .ForMember(dest => dest.SmartSortinoSqrt2, opt => opt.Ignore())
            .ForMember(dest => dest.Omega, opt => opt.Ignore())
            .ForMember(dest => dest.R2, opt => opt.Ignore())
            .ForMember(dest => dest.Skew, opt => opt.Ignore())
            .ForMember(dest => dest.Kurtosis, opt => opt.Ignore())
            .ForMember(dest => dest.KellyCriterion, opt => opt.Ignore())
            .ForMember(dest => dest.RiskOfRuin, opt => opt.Ignore())
            .ForMember(dest => dest.ExpectedShortfallCVaR, opt => opt.Ignore())
            .ForMember(dest => dest.ProfitFactor, opt => opt.Ignore())
            .ForMember(dest => dest.CommonSenseRatio, opt => opt.Ignore())
            .ForMember(dest => dest.CPCIndex, opt => opt.Ignore())
            .ForMember(dest => dest.TailRatio, opt => opt.Ignore())
            .ForMember(dest => dest.OutlierWinRatio, opt => opt.Ignore())
            .ForMember(dest => dest.OutlierLossRatio, opt => opt.Ignore())
            .ForMember(dest => dest.UlcerIndex, opt => opt.Ignore())
            .ForMember(dest => dest.SerenityIndex, opt => opt.Ignore())
            .ReverseMap();

        CreateMap<PortfolioMetrics, PortfolioMetricsDTO>()
            .ForMember(dest => dest.Benchmark, opt => opt.MapFrom(src => src.Benchmark))
            .ForMember(dest => dest.Strategy, opt => opt.MapFrom(src => src.Strategy))
            .ReverseMap();
    }
}