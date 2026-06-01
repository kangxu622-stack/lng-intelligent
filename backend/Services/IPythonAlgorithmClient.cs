using backend.Dtos.Simulation;
using backend.Models;

namespace backend.Services;

public interface IPythonAlgorithmClient
{
    Task<ScheduleCalculationResult> RunSimulationAsync(AlgorithmExecuteRequest request, CancellationToken cancellationToken = default);
}
