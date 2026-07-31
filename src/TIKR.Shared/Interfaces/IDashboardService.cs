using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
