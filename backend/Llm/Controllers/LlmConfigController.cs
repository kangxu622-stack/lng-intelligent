using backend.Dtos;
using backend.Llm.Dtos;
using backend.Llm.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Llm.Controllers;

[ApiController]
[Route("api/llm/config")]
public sealed class LlmConfigController : ControllerBase
{
    private readonly LlmConfigManager _configManager;
    private readonly LlmProviderSelector _selector;

    public LlmConfigController(LlmConfigManager configManager, LlmProviderSelector selector)
    {
        _configManager = configManager;
        _selector = selector;
    }

    [HttpGet]
    public IActionResult GetConfig()
    {
        var config = _configManager.GetConfig();
        return Ok(ApiResponse<LlmConfigDto>.Success(new LlmConfigDto
        {
            ActiveProvider = _selector.GetActiveProvider(),
            ActiveModel = _selector.GetActiveModel(),
            HasApiKey = !string.IsNullOrWhiteSpace(config.ApiKey),
            OpenAiBaseUrl = config.BaseUrl ?? "https://api.openai.com/v1/",
            OpenAiModel = config.Model ?? "deepseek-chat"
        }));
    }

    [HttpPut]
    public IActionResult UpdateConfig([FromBody] UpdateLlmConfigInput input)
    {
        if (input == null)
        {
            return Ok(ApiResponse.Error(400, "Request body is required."));
        }

        _configManager.UpdateConfig(input);
        return Ok(ApiResponse.Success(message: "配置已更新"));
    }
}
