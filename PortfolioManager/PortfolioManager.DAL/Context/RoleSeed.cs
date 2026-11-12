using Microsoft.AspNetCore.Identity;

namespace PortfolioManager.DAL.Context;

/// <summary>
/// The roles seeded by <see cref="PortfolioManagerDbContext"/>.
/// The ids are copied from the migration that last inserted the roles (20240901145117_update stockDataHistory)
/// and must never change: a new id makes the next migration delete and re-insert the role,
/// and deleting a role removes every user's assignment to it (AspNetUserRoles cascades).
/// </summary>
public static class RoleSeed
{
    public const string InvestorRoleName = "investor";
    public const string AdminRoleName = "admin";

    public const string InvestorRoleId = "0a1cde23-4095-47e3-b17e-5ae66c7c2cc7";
    public const string AdminRoleId = "04c7f224-83d2-471b-981d-08b35962f91b";

    /// <summary>New instances on every call, so EF Core never shares tracked seed objects between models.</summary>
    public static IdentityRole[] Roles() =>
    [
        new IdentityRole
        {
            Id = InvestorRoleId,
            Name = InvestorRoleName,
            NormalizedName = "INVESTOR",
            // The migrations seeded the roles without a concurrency stamp; keep it null so the model matches them.
            ConcurrencyStamp = null
        },
        new IdentityRole
        {
            Id = AdminRoleId,
            Name = AdminRoleName,
            NormalizedName = "ADMIN",
            ConcurrencyStamp = null
        }
    ];
}
