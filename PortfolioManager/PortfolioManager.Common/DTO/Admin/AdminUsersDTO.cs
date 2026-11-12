namespace PortfolioManager.Common.DTO.Admin;

/// <summary>The admin dashboard: totals and one row per user. Profile and counts only, never anybody's portfolio contents.</summary>
public class AdminUsersDTO
{
    public AdminStatsDTO Stats { get; set; } = new();
    public IList<AdminUserDTO> Users { get; set; } = new List<AdminUserDTO>();
}

public class AdminStatsDTO
{
    public int TotalUsers { get; set; }
    public int Admins { get; set; }

    /// <summary>Users who signed in or refreshed their session during the last 7 days.</summary>
    public int ActiveLast7Days { get; set; }

    public int TotalPortfolios { get; set; }
}

public class AdminUserDTO
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int PortfolioCount { get; set; }

    /// <summary>UTC. When the user's tokens were last issued (sign-in or refresh); null for an account that never signed in.</summary>
    public DateTime? LastActiveAt { get; set; }
}
