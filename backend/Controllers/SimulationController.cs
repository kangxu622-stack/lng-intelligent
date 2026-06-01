using backend.Dtos;
using backend.Dtos.Simulation;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly ISimulationOrchestrator _simulationOrchestrator;
    private readonly ISchedulePlanAppService _schedulePlanAppService;

    public SimulationController(
        ISimulationOrchestrator simulationOrchestrator,
        ISchedulePlanAppService schedulePlanAppService)
    {
        _simulationOrchestrator = simulationOrchestrator;
        _schedulePlanAppService = schedulePlanAppService;
    }

    [HttpPost]
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleCalculationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calculate(
        [FromBody] ScheduleCalculationInput request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<ScheduleCalculationResult>.Error(400, "Calculate request body must be provided."));
        }

        if (string.IsNullOrWhiteSpace(request.PlanName))
        {
            return BadRequest(ApiResponse<ScheduleCalculationResult>.Error(400, "PlanName is required."));
        }

        try
        {
            var result = await _simulationOrchestrator.CalculateAsync(request, cancellationToken);
            return Ok(ApiResponse<ScheduleCalculationResult>.Success(result, "计算成功"));
        }
        catch (HttpRequestException httpEx)
        {
            return StatusCode(502, ApiResponse<ScheduleCalculationResult>.Error(502, $"Python service error: {httpEx.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ScheduleCalculationResult>.Error(500, ex.Message));
        }
    }

    [HttpPost("save")]
    [ProducesResponseType(typeof(ApiResponse<SaveScheduleResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save(
        [FromBody] SaveScheduleInput request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(400, "Save request body must be provided."));
        }

        if (request.CreatedBy <= 0)
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(400, "CreatedBy is required."));
        }

        if (string.IsNullOrWhiteSpace(request.PlanName))
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(400, "PlanName is required."));
        }

        if (request.CalculateInput == null)
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(400, "CalculateInput is required."));
        }

        if (request.SimulationResult == null)
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(400, "SimulationResult is required."));
        }

        try
        {
            var result = await _schedulePlanAppService.SaveAsync(request, cancellationToken);
            return Ok(ApiResponse<SaveScheduleResult>.Success(result, "保存成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<SaveScheduleResult>.Error(500, ex.Message));
        }
    }
}
