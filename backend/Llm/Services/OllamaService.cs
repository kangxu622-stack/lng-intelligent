using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace backend.Llm.Services;

public sealed class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OllamaChatResult> ChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = model,
            Stream = false,
            Messages = messages
                .Select(m => new OllamaChatRequestMessage
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToList()
        };

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Ollama returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Ollama returned {(int)response.StatusCode}: {errorContent}");
        }

        var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return new OllamaChatResult
        {
            Model = payload.Model ?? model,
            Content = payload.Message?.Content ?? string.Empty,
            PromptTokens = payload.PromptEvalCount,
            CompletionTokens = payload.EvalCount,
            TotalTokens = (payload.PromptEvalCount ?? 0) + (payload.EvalCount ?? 0),
            ResponseMs = (int)stopwatch.ElapsedMilliseconds
        };
    }

    public async IAsyncEnumerable<OllamaStreamChunk> StreamChatAsync(
        string model,
        IReadOnlyList<OllamaChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = model,
            Stream = true,
            Messages = messages
                .Select(m => new OllamaChatRequestMessage
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Ollama stream returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Ollama returned {(int)response.StatusCode}: {errorContent}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
          var line = await reader.ReadLineAsync(cancellationToken);
          if (string.IsNullOrWhiteSpace(line))
          {
              continue;
          }

          var payload = System.Text.Json.JsonSerializer.Deserialize<OllamaStreamResponse>(line);
          if (payload == null)
          {
              continue;
          }

          yield return new OllamaStreamChunk
          {
              Model = payload.Model ?? model,
              ContentDelta = payload.Message?.Content ?? string.Empty,
              Done = payload.Done,
              PromptTokens = payload.PromptEvalCount,
              CompletionTokens = payload.EvalCount,
              TotalTokens = (payload.PromptEvalCount ?? 0) + (payload.EvalCount ?? 0)
          };
        }
    }

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaChatRequestMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaChatRequestMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("message")]
        public OllamaChatResponseMessage? Message { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }
    }

    private sealed class OllamaChatResponseMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class OllamaStreamResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("message")]
        public OllamaChatResponseMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }
    }
}
