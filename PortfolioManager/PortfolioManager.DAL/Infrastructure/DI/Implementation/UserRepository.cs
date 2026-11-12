using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class UserRepository(PortfolioManagerDbContext context) : IUserRepository
{
    public async Task<IReadOnlyList<UserSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        // Three plain queries, merged here: the count and the role names of every user come from one query each,
        // not from one query per user.
        var users = await context.Users.AsNoTracking()
            .Select(user => new { user.Id, user.FullName, user.Email, user.TokenCreated })
            .ToListAsync(cancellationToken);

        var portfolioCounts = await context.Portfolios.AsNoTracking()
            .GroupBy(portfolio => portfolio.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.UserId, row => row.Count, cancellationToken);

        var roles = (await (from assignment in context.UserRoles.AsNoTracking()
                join role in context.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                select new { assignment.UserId, role.Name })
            .ToListAsync(cancellationToken))
            .ToLookup(row => row.UserId, row => row.Name!);

        return users
            .Select(user => new UserSummary(
                user.Id,
                user.FullName,
                user.Email,
                user.TokenCreated,
                portfolioCounts.GetValueOrDefault(user.Id),
                roles[user.Id].OrderBy(name => name).ToList()))
            .ToList();
    }
}
