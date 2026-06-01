using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Llm.Services;

public sealed class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmConfigManager _configManager;
    private readonly ILogger<OpenAiProvider> _logger;

    public OpenAiProvider(HttpClient httpClient, LlmConfigManager configManager, ILogger<OpenAiProvider> logger)
    {
        _httpClient = httpClient;
        _configManager = configManager;
        _logger = logger;
    }

    private void ApplyAuthHeader()
    {
        var (apiKey, _, _) = _configManager.GetConfig();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey ?? string.Empty);
    }

    private string GetBaseUrl()
    {
        var (_, baseUrl, _) = _configManager.GetConfig();
        return (baseUrl ?? "https://api.openai.com/v1/").TrimEnd('/');
    }

    private string GetModel()
    {
        var (_, _, model) = _configManager.GetConfig();
        return model ?? "gpt-4o-mini";
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OllamaChatResult> ChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var baseUrl = GetBaseUrl();
        var request = new ChatCompletionRequest
        {
            Model = string.IsNullOrWhiteSpace(model) ? GetModel() : model,
            Stream = false,
            Messages = messages
                .Select(m => new ChatMessage { Role = m.Role, Content = m.Content })
                .ToList()
        };

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/chat/completions", request, cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OpenAI returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"OpenAI returned {(int)response.StatusCode}: {errorContent}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OpenAI returned an empty response.");

        var choice = payload.Choices?.FirstOrDefault();
        return new OllamaChatResult
        {
            Model = payload.Model ?? model,
            Content = choice?.Message?.Content ?? string.Empty,
            PromptTokens = payload.Usage?.PromptTokens,
            CompletionTokens = payload.Usage?.CompletionTokens,
            TotalTokens = payload.Usage?.TotalTokens,
            ResponseMs = (int)stopwatch.ElapsedMilliseconds
        };
    }

    public async IAsyncEnumerable<OllamaStreamChunk> StreamChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var baseUrl = GetBaseUrl();
        var request = new ChatCompletionRequest
        {
            Model = string.IsNullOrWhiteSpace(model) ? GetModel() : model,
            Stream = true,
            Messages = messages
                .Select(m => new ChatMessage { Role = m.Role, Content = m.Content })
                .ToList()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = content
        };
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OpenAI stream returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"OpenAI returned {(int)response.StatusCode}: {errorContent}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (!line.StartsWith("data: "))
                continue;

            var data = line[6..];
            if (data == "[DONE]")
                break;

            var payload = JsonSerializer.Deserialize<StreamResponse>(data);
            if (payload == null)
                continue;

            var delta = payload.Choices?.FirstOrDefault()?.Delta;
            yield return new OllamaStreamChunk
            {
                Model = payload.Model ?? model,
                ContentDelta = delta?.Content ?? string.Empty,
                Done = payload.Choices?.FirstOrDefault()?.FinishReason is not null,
                PromptTokens = payload.Usage?.PromptTokens,
                CompletionTokens = payload.Usage?.CompletionTokens,
                TotalTokens = payload.Usage?.TotalTokens
            };
        }
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }

        [JsonPropertyName("delta")]
        public ChatMessage? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private sealed class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }
    }

    private sealed class StreamResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }
}
