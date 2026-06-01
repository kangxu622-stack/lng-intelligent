using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentAppService _equipmentService;

    public EquipmentController(IEquipmentAppService equipmentService)
    {
        _equipmentService = equipmentService;
    }
    [HttpGet("counts")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentCounts>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCounts(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _equipmentService.GetCountsAsync(cancellationToken);
            return Ok(ApiResponse<EquipmentCounts>.Success(result, "获取设备数量成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<EquipmentCounts>.Error(500, $"获取设备数量失败: {ex.Message}"));
        }
    }

// 设备列表（分页、筛选）
    [HttpGet("list")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] EquipmentQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _equipmentService.GetEquipmentListAsync(query, cancellationToken);
            return Ok(ApiResponse<EquipmentListResponse>.Success(result, "获取设备列表成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<EquipmentListResponse>.Error(500, $"获取设备列表失败: {ex.Message}"));
        }
    }

    // 单个设备详情
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _equipmentService.GetEquipmentByIdAsync(id, cancellationToken);
            if (result == null)
                return Ok(ApiResponse<EquipmentDto>.Error(404, "设备不存在"));
            return Ok(ApiResponse<EquipmentDto>.Success(result, "获取设备详情成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<EquipmentDto>.Error(500, $"获取设备详情失败: {ex.Message}"));
        }
    }

    // 设备类型列表
    [HttpGet("types")]
    [ProducesResponseType(typeof(ApiResponse<List<EquipmentTypeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _equipmentService.GetEquipmentTypesAsync(cancellationToken);
            return Ok(ApiResponse<List<EquipmentTypeDto>>.Success(result, "获取设备类型成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<List<EquipmentTypeDto>>.Error(500, $"获取设备类型失败: {ex.Message}"));
        }
    }

    [HttpGet("monitoring")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentMonitoringResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonitoring([FromQuery] EquipmentMonitoringQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _equipmentService.GetEquipmentMonitoringAsync(query, cancellationToken);
            return Ok(ApiResponse<EquipmentMonitoringResponse>.Success(result, "设备监测数据查询成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<EquipmentMonitoringResponse>.Error(500, $"设备监测数据查询失败: {ex.Message}"));
        }
    }

    [HttpGet("monitoring/{id}/history")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentMonitoringTrendResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonitoringHistory(
        int id,
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _equipmentService.GetEquipmentMonitoringTrendAsync(id, start, end, cancellationToken);
            if (result == null)
            {
                return Ok(ApiResponse<EquipmentMonitoringTrendResponse>.Error(404, "设备不存在"));
            }

            return Ok(ApiResponse<EquipmentMonitoringTrendResponse>.Success(result, "设备监测趋势查询成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<EquipmentMonitoringTrendResponse>.Error(500, $"设备监测趋势查询失败: {ex.Message}"));
        }
    }

}
