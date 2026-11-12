using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PortfolioManager.API.Filters;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Services;
using PortfolioManager.BLL.Services.Auth;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Entities.Auth;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation;
using PortfolioManager.MathModule.Client;


public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Secrets are not in appsettings: locally they come from `dotnet user-secrets`, on Azure from the
        // App Service settings (see PortfolioManager/README.md). Fail at startup with the setting's name.
        var connectionString = builder.Configuration.GetConnectionString("PortfolioManagerDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:PortfolioManagerDb is not configured.");

        builder.Services.AddDbContext<PortfolioManagerDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        builder.Services.AddScoped<IStockSymbolRepository, StockSymbolRepository>();
        builder.Services.AddScoped<IStockRepository, StockRepository>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IPortfolioService, PortfolioService>();
        builder.Services.AddScoped<IPortfolioStatisticService, PortfolioStatisticService>();
        builder.Services.AddScoped<IStockService, StockService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        // Python math service: MathModule:BaseUrl is required (startup fails without it), MathModule:ApiKey is a secret.
        builder.Services.AddMathModuleClient(builder.Configuration);
        // Allowed origins can be set as Cors__Origins__0, Cors__Origins__1, ... in the App Service settings.
        var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                          ?? new[] { "https://portfoliomanagerapi.azurewebsites.net", "https://portfoliomanagerclient.azurewebsites.net" };
        if (builder.Environment.IsDevelopment())
            corsOrigins = corsOrigins.Append("http://localhost:3000").ToArray();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: "AllowAllOrigins",
                builder =>
                {
                    builder.WithOrigins(corsOrigins);
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowCredentials();
                });
        });

        builder.Services
            .AddControllers(options => options.Filters.Add<PortfolioManagerExceptionFilterAttribute>())
            // Without this the JSON parser's own message ("could not be converted to System.Int32. Path: $.quantity
            // | LineNumber: ...") ends up in the 400. ModelStateErrorResponse words it for the user instead.
            .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false)
            // [ApiController] answers an invalid request with 400 by itself; this gives that 400 the same body as every other error.
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = ModelStateErrorResponse.Create);


        #region Auth
        var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);
        builder.Services.AddSingleton(Options.Create(jwtSettings));
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                // Defaults made explicit: 5 wrong passwords lock the account for 5 minutes.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<PortfolioManagerDbContext>()
            .AddErrorDescriber<UkrainianIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        builder.Services
            // Roles are checked where they matter, with [Authorize(Roles = ...)] on the action
            // (see UserController.GetAllUsersAsync). The unused "ElevatedRights" policy is gone.
            .AddAuthorization()
            // JWT bearer is the only way in. There is no cookie or external (Google) sign-in scheme.
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
            opt.TokenLifespan = TimeSpan.FromHours(1));
        #endregion

        // Swagger describes every route and lets anyone try them, so it exists only while developing:
        // it is not even registered in any other environment.
        if (builder.Environment.IsDevelopment())
        {
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
        }

        var app = builder.Build();

        var service = (IServiceScopeFactory)app.Services.GetService(typeof(IServiceScopeFactory))!;

        await using (var db = service.CreateScope().ServiceProvider.GetRequiredService<PortfolioManagerDbContext>())
        {
            // SQL Server in every real run. The in-memory database the HTTP tests swap in has no migrations.
            if (db.Database.IsRelational())
                await db.Database.MigrateAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        app.UseCors("AllowAllOrigins");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.RunAsync();
    }
}

