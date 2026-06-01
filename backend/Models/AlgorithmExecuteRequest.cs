using System.Text.Json.Serialization;

namespace backend.Models;

public sealed class AlgorithmExecuteRequest
{
    public WeatherData? WeatherData { get; set; }

    public UnloadPlan? UnloadPlan { get; set; }

    public Dictionary<string, object?>? AlgorithmParams { get; set; }

    [JsonPropertyName("save_dir")]
    public string? SaveDir { get; set; }

    [JsonPropertyName("run_pso")]
    public bool RunPso { get; set; } = true;

    [JsonPropertyName("pso_particles")]
    public int PsoParticles { get; set; } = 40;

    [JsonPropertyName("pso_iters")]
    public int PsoIters { get; set; } = 20;
}
