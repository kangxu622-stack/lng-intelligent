using backend.Dtos;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;

namespace backend.Controllers;

[ApiController]
[Route("api/schedules")]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleQueryRepository _queryRepo;
    private readonly ISchedulePlanRepository _planRepo;

    public SchedulesController(IScheduleQueryRepository queryRepo, ISchedulePlanRepository planRepo)
    {
        _queryRepo = queryRepo;
        _planRepo = planRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ScheduleQueryDto query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _queryRepo.GetSchedulePlansAsync(
            query.Status, query.PageIndex, query.PageSize, cancellationToken);

        var dtos = items.Select(p => new SchedulePlanDto
        {
            PlanId = p.PlanId,
            PlanName = p.PlanName,
            PlanCode = p.PlanCode,
            CreatedBy = p.CreatedBy,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Status = p.Status,
            CalculationMode = p.CalculationMode,
            TotalOutputM3 = p.TotalOutputM3,
            TotalPowerKwh = p.TotalPowerKwh,
            TotalCostCny = p.TotalCostCny,
            OptimizationScore = p.OptimizationScore,
            FitnessValue = p.FitnessValue
        }).ToList();

        var result = new SchedulePlanListResponse
        {
            Items = dtos,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<SchedulePlanListResponse>.Success(result, "Schedule plans loaded successfully."));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
    {
        var plan = await _queryRepo.GetPlanByIdAsync(id, cancellationToken);
        if (plan == null)
        {
            return Ok(ApiResponse<SchedulePlanDetailResponse>.Error(404, "Schedule plan was not found."));
        }

        var details = await _queryRepo.GetDetailsByPlanIdAsync(id, cancellationToken);
        var reportSheets = await _queryRepo.GetReportSheetsByPlanIdAsync(id, cancellationToken);

        var planDto = new SchedulePlanDto
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            PlanCode = plan.PlanCode,
            CreatedBy = plan.CreatedBy,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            Status = plan.Status,
            CalculationMode = plan.CalculationMode,
            TotalOutputM3 = plan.TotalOutputM3,
            TotalPowerKwh = plan.TotalPowerKwh,
            TotalCostCny = plan.TotalCostCny,
            OptimizationScore = plan.OptimizationScore,
            FitnessValue = plan.FitnessValue
        };

        var detailDtos = details.Select(d => new SchedulePlanDetailDto
        {
            DetailId = d.DetailId,
            Hour = d.Hour,
            EquipmentId = d.EquipmentId,
            EquipmentCode = d.EquipmentCode,
            DeviceGroup = d.DeviceGroup,
            Action = d.Action,
            SequenceOrder = d.SequenceOrder,
            DelayTimeSec = d.DelayTimeSec,
            TargetFlowM3h = d.TargetFlowM3h,
            TargetLoadPct = d.TargetLoadPct,
            TargetPressureMpa = d.TargetPressureMpa
        }).ToList();

        var result = new SchedulePlanDetailResponse
        {
            Plan = planDto,
            Details = detailDtos,
            ReportSheets = reportSheets
        };

        return Ok(ApiResponse<SchedulePlanDetailResponse>.Success(result, "Schedule plan detail loaded successfully."));
    }

    [HttpGet("{id}/export/csv")]
    public async Task<IActionResult> ExportCsv(int id, CancellationToken cancellationToken)
    {
        var plan = await _queryRepo.GetPlanByIdAsync(id, cancellationToken);
        if (plan == null)
        {
            return Ok(ApiResponse.Error(404, "Schedule plan was not found."));
        }

        var reportSheets = await _queryRepo.GetReportSheetsByPlanIdAsync(id, cancellationToken);
        if (reportSheets.Count == 0)
        {
            return Ok(ApiResponse.Error(404, "No report data is available for export."));
        }

        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var sheet in reportSheets)
            {
                var entry = archive.CreateEntry($"{SanitizeFileName(sheet.SheetName)}.csv");
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream, new UTF8Encoding(true));

                await writer.WriteLineAsync(string.Join(",", sheet.Columns.Select(ToCsvCell)));
                foreach (var row in sheet.Rows)
                {
                    var values = sheet.Columns.Select(column =>
                    {
                        row.TryGetValue(column, out var value);
                        return ToCsvCell(value);
                    });

                    await writer.WriteLineAsync(string.Join(",", values));
                }

                await writer.FlushAsync();
            }
        }

        stream.Position = 0;
        var fileName = $"{SanitizeFileName(plan.PlanName ?? plan.PlanCode ?? $"schedule-{id}")}-csv.zip";
        return File(stream.ToArray(), "application/zip", fileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _planRepo.DeleteSchedulePlanAsync(id, cancellationToken);
            if (!deleted)
            {
                return Ok(ApiResponse.Error(404, "Schedule plan was not found."));
            }

            return Ok(ApiResponse.Success(message: "Schedule plan deleted successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, ex.Message));
        }
    }

    private static string ToCsvCell(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var text = value switch
        {
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            _ => value.ToString() ?? string.Empty
        };

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }
}
