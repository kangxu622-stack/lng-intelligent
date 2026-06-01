using backend.Dtos.Simulation;

namespace backend.Services;

public interface IParameterQueryService
{
    Task<Dictionary<string, object?>> BuildAlgorithmParamsAsync(ScheduleCalculationInput request, CancellationToken cancellationToken);
}
