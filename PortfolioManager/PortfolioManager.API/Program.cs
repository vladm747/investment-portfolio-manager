using System.Text;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Profiles;
using PortfolioManager.BLL.Services;
using PortfolioManager.BLL.Services.Auth;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities.Auth;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation;
using YahooFinance.Client.Interfaces;
using YahooFinance.Client.Services;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<PortfolioManagerDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("PortfolioManagerDb")));

        builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));
        
        builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        builder.Services.AddScoped<IStockSymbolRepository, StockSymbolRepository>();
        builder.Services.AddScoped<IStockRepository, StockRepository>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IPortfolioService, PortfolioService>();
        builder.Services.AddScoped<IStockService, StockService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IYahooFinanceService, YahooFinanceService>();

        builder.Services.AddControllers();

        #region Auth
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("Jwt"));
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
        
        builder.Services
            .AddAuthorization(options => 
                options.AddPolicy("ElevatedRights", policy =>
                    policy.RequireRole("investor", "admin")))
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings!.Issuer,
                    ValidAudience = jwtSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        
        builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<PortfolioManagerDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
            opt.TokenLifespan = TimeSpan.FromHours(1));
        
    

      

        #endregion
        
        // Add services to the container.
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT containing userid claim",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
            });
            var security =
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                            UnresolvedReference = true
                        },
                        new List<string>()
                    }
                };
            c.AddSecurityRequirement(security);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        await app.RunAsync();
    }
}

