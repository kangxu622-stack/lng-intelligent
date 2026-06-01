using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Dtos.Simulation;

public sealed class ScheduleCalculationResult
{
    public JsonElement Summary { get; set; }

    public JsonElement Hourly { get; set; }

    public JsonElement Unitized { get; set; }

    public JsonElement Labels { get; set; }

    [JsonPropertyName("tank_levels")]
    public JsonElement TankLevels { get; set; }

    public JsonElement Bog { get; set; }

    [JsonPropertyName("run_history")]
    public List<double>? RunHistory { get; set; }
}
