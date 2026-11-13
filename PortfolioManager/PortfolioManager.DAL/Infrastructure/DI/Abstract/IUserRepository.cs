namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

/// <summary>
/// One user as the admin dashboard needs them: the profile and two counts, nothing else.
/// The password hash, the refresh token and the lockout state are not selected at all.
/// </summary>
/// <param name="LastTokenIssuedAt">Kind is unspecified, as the database returns it; <c>default</c> for a user who never signed in.</param>
public sealed record UserSummary(
    string Id,
    string FullName,
    string? Email,
    DateTime LastTokenIssuedAt,
    int PortfolioCount,
    IReadOnlyList<string> Roles);

/// <summary>Reads about users that <c>UserManager</c> cannot do in one query. Accounts themselves are changed through <c>UserManager</c>.</summary>
public interface IUserRepository
{
    /// <summary>Every user with their roles and the number of their portfolios, in three queries however many users there are.</summary>
    Task<IReadOnlyList<UserSummary>> GetSummariesAsync(CancellationToken cancellationToken = default);
}
