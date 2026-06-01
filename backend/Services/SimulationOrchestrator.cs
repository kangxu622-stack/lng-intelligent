using System.Globalization;
using backend.Dtos.Simulation;
using backend.Models;

namespace backend.Services;

public sealed class SimulationOrchestrator : ISimulationOrchestrator
{
    private readonly IPythonAlgorithmClient _pythonAlgorithmClient;
    private readonly IParameterQueryService _parameterQueryService;

    public SimulationOrchestrator(
        IPythonAlgorithmClient pythonAlgorithmClient,
        IParameterQueryService parameterQueryService)
    {
        _pythonAlgorithmClient = pythonAlgorithmClient;
        _parameterQueryService = parameterQueryService;
    }

    public async Task<AlgorithmExecuteRequest> BuildAlgorithmExecuteRequestAsync(
        ScheduleCalculationInput request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var algorithmParams = await _parameterQueryService.BuildAlgorithmParamsAsync(request, cancellationToken);
        var executeRequest = new AlgorithmExecuteRequest
        {
            WeatherData = null,
            UnloadPlan = null,
            AlgorithmParams = algorithmParams,
            SaveDir = BuildSaveDirectory(request.PlanName),
            RunPso = true,
            PsoParticles = 40,
            PsoIters = 20
        };

        return executeRequest;
    }

    public async Task<ScheduleCalculationResult> CalculateAsync(
        ScheduleCalculationInput request,
        CancellationToken cancellationToken)
    {
        var executeRequest = await BuildAlgorithmExecuteRequestAsync(request, cancellationToken);
        return await _pythonAlgorithmClient.RunSimulationAsync(executeRequest, cancellationToken);
    }

    private static string BuildSaveDirectory(string? planName)
    {
        var safePlanName = SanitizePathSegment(planName);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(AppContext.BaseDirectory, "simulation-output", $"{safePlanName}-{timestamp}");
    }

    private static string SanitizePathSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "simulation";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim().Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "simulation" : sanitized;
    }
}
