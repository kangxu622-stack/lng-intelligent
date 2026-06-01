using backend.Dtos;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/alarms")]
public class AlarmsController : ControllerBase
{
    private readonly IAlarmRepository _alarmRepo;

    public AlarmsController(IAlarmRepository alarmRepo)
    {
        _alarmRepo = alarmRepo;
    }

    // 告警配置列表
    [HttpGet("config")]
    public async Task<IActionResult> GetConfigs(CancellationToken cancellationToken)
    {
        var list = await _alarmRepo.GetAlarmConfigsAsync(cancellationToken);
        var dtos = list.Select(c => new AlarmConfigDto
        {
            AlarmConfigId = c.AlarmConfigId,
            AlarmCode = c.AlarmCode,
            AlarmName = c.AlarmName,
            AlarmType = c.AlarmType,
            EquipmentId = c.EquipmentId,
            TagId = c.TagId,
            ThresholdValue = c.ThresholdValue,
            Hysteresis = c.Hysteresis,
            Priority = c.Priority,
            Enabled = c.Enabled,
            Description = c.Description
        }).ToList();
        return Ok(ApiResponse<AlarmConfigListResponse>.Success(
            new AlarmConfigListResponse { Items = dtos }, "获取告警配置成功"));
    }

    // 活跃告警（未结束且未确认）
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var list = await _alarmRepo.GetActiveAlarmsAsync(cancellationToken);
        var dtos = list.Select(h => new AlarmHistoryDto
        {
            AlarmId = h.AlarmId,
            AlarmConfigId = h.AlarmConfigId,
            EquipmentId = h.EquipmentId,
            TagId = h.TagId,
            AlarmLevel = h.AlarmLevel,
            AlarmValue = h.AlarmValue,
            ThresholdValue = h.ThresholdValue,
            StartTime = h.StartTime,
            EndTime = h.EndTime,
            DurationSec = h.DurationSec,
            AckTime = h.AckTime,
            AckBy = h.AckBy
        }).ToList();
        return Ok(ApiResponse<AlarmHistoryListResponse>.Success(
            new AlarmHistoryListResponse { Items = dtos, TotalCount = dtos.Count }, "获取活跃告警成功"));
    }

    // 告警历史（分页筛选）
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] AlarmQueryDto query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _alarmRepo.GetAlarmHistoryAsync(
            query.EquipmentId, query.TagId, query.StartTime, query.EndTime,
            query.PageIndex, query.PageSize, cancellationToken);

        var dtos = items.Select(h => new AlarmHistoryDto
        {
            AlarmId = h.AlarmId,
            AlarmConfigId = h.AlarmConfigId,
            EquipmentId = h.EquipmentId,
            TagId = h.TagId,
            AlarmLevel = h.AlarmLevel,
            AlarmValue = h.AlarmValue,
            ThresholdValue = h.ThresholdValue,
            StartTime = h.StartTime,
            EndTime = h.EndTime,
            DurationSec = h.DurationSec,
            AckTime = h.AckTime,
            AckBy = h.AckBy
        }).ToList();

        var result = new AlarmHistoryListResponse { Items = dtos, TotalCount = totalCount };
        return Ok(ApiResponse<AlarmHistoryListResponse>.Success(result, "获取告警历史成功"));
    }
}