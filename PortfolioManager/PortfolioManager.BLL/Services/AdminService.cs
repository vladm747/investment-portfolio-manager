using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO.Admin;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;

namespace PortfolioManager.BLL.Services;

public class AdminService(IUserRepository users, TimeProvider time) : IAdminService
{
    /// <summary>How recently a user must have been active to count as active.</summary>
    public static readonly TimeSpan ActiveWindow = TimeSpan.FromDays(7);

    public async Task<AdminUsersDTO> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await users.GetSummariesAsync(cancellationToken);
        var activeSince = time.GetUtcNow().UtcDateTime - ActiveWindow;

        var rows = summaries
            .Select(user => new AdminUserDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = user.Roles.ToList(),
                PortfolioCount = user.PortfolioCount,
                LastActiveAt = LastActive(user.LastTokenIssuedAt)
            })
            // Whoever was here most recently first; accounts that never signed in last.
            .OrderByDescending(row => row.LastActiveAt.HasValue)
            .ThenByDescending(row => row.LastActiveAt)
            .ThenBy(row => row.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AdminUsersDTO
        {
            Stats = new AdminStatsDTO
            {
                TotalUsers = rows.Count,
                Admins = rows.Count(row => row.Roles.Contains(RoleSeed.AdminRoleName)),
                ActiveLast7Days = rows.Count(row => row.LastActiveAt >= activeSince),
                TotalPortfolios = rows.Sum(row => row.PortfolioCount)
            },
            Users = rows
        };
    }

    /// <summary>The tokens' creation time is written whenever tokens are issued (sign-in or refresh); an account that never had any keeps the default.</summary>
    private static DateTime? LastActive(DateTime tokenCreated) =>
        tokenCreated == default ? null : DateTime.SpecifyKind(tokenCreated, DateTimeKind.Utc);
}
