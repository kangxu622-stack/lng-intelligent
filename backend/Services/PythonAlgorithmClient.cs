using backend.Dtos.Simulation;
using backend.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace backend.Services;

public sealed class PythonAlgorithmClient : IPythonAlgorithmClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PythonAlgorithmClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ScheduleCalculationResult> RunSimulationAsync(AlgorithmExecuteRequest request, CancellationToken cancellationToken = default)
    {
        using var content = JsonContent.Create(request, options: CamelCaseOptions);
        using var response = await _httpClient.PostAsync("simulate", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Python service returned {(int)response.StatusCode}: {response.ReasonPhrase}. {errorContent}");
        }

        var simulationResponse = await response.Content.ReadFromJsonAsync<ScheduleCalculationResult>(CamelCaseOptions, cancellationToken);
        return simulationResponse ?? throw new InvalidOperationException("Python service returned an empty response.");
    }
}
