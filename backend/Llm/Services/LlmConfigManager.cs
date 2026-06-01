using backend.Llm.Dtos;
using System.Text.Json;

namespace backend.Llm.Services;

public sealed class LlmConfigManager
{
    private readonly string _configPath;
    private readonly object _lock = new();

    public string? ApiKey { get; private set; }
    public string? BaseUrl { get; private set; }
    public string? Model { get; private set; }

    public LlmConfigManager(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configPath = Path.Combine(environment.ContentRootPath, "llm_config.json");
        LoadFromFile();

        if (string.IsNullOrWhiteSpace(ApiKey))
            ApiKey = configuration["Llm:OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(BaseUrl))
            BaseUrl = configuration["Llm:OpenAI:BaseUrl"] ?? "https://api.deepseek.com/v1/";
        if (string.IsNullOrWhiteSpace(Model))
            Model = configuration["Llm:OpenAI:Model"] ?? "deepseek-chat";
    }

    public (string? ApiKey, string? BaseUrl, string? Model) GetConfig()
    {
        lock (_lock)
        {
            return (ApiKey, BaseUrl, Model);
        }
    }

    public void UpdateConfig(UpdateLlmConfigInput input)
    {
        lock (_lock)
        {
            if (input.ApiKey != null)
                ApiKey = string.IsNullOrWhiteSpace(input.ApiKey) ? null : input.ApiKey.Trim();
            if (input.BaseUrl != null)
                BaseUrl = string.IsNullOrWhiteSpace(input.BaseUrl) ? "https://api.deepseek.com/v1/" : input.BaseUrl.Trim();
            if (input.Model != null)
                Model = string.IsNullOrWhiteSpace(input.Model) ? "deepseek-chat" : input.Model.Trim();
        }

        SaveToFile();
    }

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_configPath))
                return;

            var json = File.ReadAllText(_configPath);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ApiKey", out var ak))
                ApiKey = ak.GetString();
            if (doc.RootElement.TryGetProperty("BaseUrl", out var bu))
                BaseUrl = bu.GetString();
            if (doc.RootElement.TryGetProperty("Model", out var m))
                Model = m.GetString();
        }
        catch
        {
            // ignore corrupted config file
        }
    }

    private void SaveToFile()
    {
        lock (_lock)
        {
            var obj = new { ApiKey, BaseUrl, Model };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
    }
}
