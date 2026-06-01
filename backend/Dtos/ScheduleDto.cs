namespace backend.Dtos;

public class SchedulePlanDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CalculationMode { get; set; }
    public decimal? TotalOutputM3 { get; set; }
    public decimal? TotalPowerKwh { get; set; }
    public decimal? TotalCostCny { get; set; }
    public decimal? OptimizationScore { get; set; }
    public decimal? FitnessValue { get; set; }
}

public class SchedulePlanDetailDto
{
    public int DetailId { get; set; }
    public int Hour { get; set; }
    public int? EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string DeviceGroup { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int DelayTimeSec { get; set; }
    public decimal? TargetFlowM3h { get; set; }
    public decimal? TargetLoadPct { get; set; }
    public decimal? TargetPressureMpa { get; set; }
}

public class SchedulePlanListResponse
{
    public List<SchedulePlanDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ScheduleReportSheetDto
{
    public string SheetName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public class SchedulePlanDetailResponse
{
    public SchedulePlanDto Plan { get; set; } = new();
    public List<SchedulePlanDetailDto> Details { get; set; } = new();
    public List<ScheduleReportSheetDto> ReportSheets { get; set; } = new();
}

public class ScheduleQueryDto
{
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
