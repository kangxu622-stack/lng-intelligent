using backend.Dtos;

namespace backend.Repositories;

public interface IScheduleQueryRepository
{
    Task<(List<SchedulePlanDto> Items, int TotalCount)> GetSchedulePlansAsync(
        string? status, int pageIndex, int pageSize, CancellationToken cancellationToken);
    Task<SchedulePlanDto?> GetPlanByIdAsync(int planId, CancellationToken cancellationToken);
    Task<List<SchedulePlanDetailDto>> GetDetailsByPlanIdAsync(int planId, CancellationToken cancellationToken);
    Task<List<ScheduleReportSheetDto>> GetReportSheetsByPlanIdAsync(int planId, CancellationToken cancellationToken);
}
