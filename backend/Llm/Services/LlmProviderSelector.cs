namespace backend.Llm.Services;

public sealed class LlmProviderSelector
{
    public string ActiveProvider { get; private set; } = "ollama";
    public string ActiveModel { get; private set; }

    private readonly OllamaProvider _ollamaProvider;
    private readonly OpenAiProvider _openAiProvider;
    private readonly LlmConfigManager _configManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmProviderSelector> _logger;

    public LlmProviderSelector(
        OllamaProvider ollamaProvider,
        OpenAiProvider openAiProvider,
        LlmConfigManager configManager,
        IConfiguration configuration,
        ILogger<LlmProviderSelector> logger)
    {
        _ollamaProvider = ollamaProvider;
        _openAiProvider = openAiProvider;
        _configManager = configManager;
        _configuration = configuration;
        _logger = logger;

        ActiveModel = _configuration["Llm:Ollama:Model"] ?? "qwen2.5:7b";
    }

    public async Task<ILlmProvider> SelectProviderAsync(CancellationToken cancellationToken = default)
    {
        if (await _ollamaProvider.IsAvailableAsync(cancellationToken))
        {
            ActiveProvider = "ollama";
            ActiveModel = _configuration["Llm:Ollama:Model"] ?? "qwen2.5:7b";
            _logger.LogInformation("LLM provider: ollama (model={Model})", ActiveModel);
            return _ollamaProvider;
        }

        var (apiKey, _, model) = _configManager.GetConfig();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            ActiveProvider = "openai";
            ActiveModel = model ?? "gpt-4o-mini";
            _logger.LogInformation("LLM provider: openai (model={Model})", ActiveModel);
            return _openAiProvider;
        }

        throw new InvalidOperationException("本地 Ollama 不可用且未配置外部 API Key，请在设置中配置 API Key。");
    }

    public string GetActiveProvider() => ActiveProvider;

    public string GetActiveModel() => ActiveModel;
}
