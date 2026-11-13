using PortfolioManager.Common.DTO.Admin;

namespace PortfolioManager.BLL.Interfaces;

public interface IAdminService
{
    /// <summary>The totals and the list of users for the admin dashboard. The caller's role is checked by the controller.</summary>
    Task<AdminUsersDTO> GetUsersAsync(CancellationToken cancellationToken = default);
}
