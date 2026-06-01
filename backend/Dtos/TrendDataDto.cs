namespace backend.Dtos;

public class TrendDataDto
{
    public string TagName { get; set; } = string.Empty;
    public double Value { get; set; }
    public int Quality { get; set; } = 192;
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TrendQueryDto
{
    public string TagName { get; set; } = string.Empty;
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 1000;
}

public class TrendDataListResponse
{
    public List<TrendDataDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}