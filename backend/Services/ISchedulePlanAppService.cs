using backend.Dtos.Simulation;

namespace backend.Services;

public interface ISchedulePlanAppService
{
    Task<SaveScheduleResult> SaveAsync(SaveScheduleInput input, CancellationToken cancellationToken);
}
