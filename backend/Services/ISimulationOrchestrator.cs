using backend.Dtos.Simulation;
using backend.Models;

namespace backend.Services;

public interface ISimulationOrchestrator
{
    Task<AlgorithmExecuteRequest> BuildAlgorithmExecuteRequestAsync(ScheduleCalculationInput request, CancellationToken cancellationToken);

    Task<ScheduleCalculationResult> CalculateAsync(ScheduleCalculationInput request, CancellationToken cancellationToken);
}
