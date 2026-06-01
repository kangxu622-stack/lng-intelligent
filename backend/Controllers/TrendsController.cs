using backend.Dtos;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrendsController : ControllerBase
{
    private readonly ITagTrendRepository _trendRepo;

    public TrendsController(ITagTrendRepository trendRepo)
    {
        _trendRepo = trendRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetTrends([FromQuery] TrendQueryDto query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.TagName))
            return Ok(ApiResponse<TrendDataListResponse>.Error(400, "TagName 必填"));

        try
        {
            var (items, totalCount) = await _trendRepo.GetHistoryAsync(
                query.TagName, query.Start, query.End,
                query.PageIndex, query.PageSize, cancellationToken);

            var dtos = items.Select(i => new TrendDataDto
            {
                TagName = i.TagName,
                Value = i.Value,
                Quality = i.Quality,
                Unit = i.Unit,
                Timestamp = i.Timestamp
            }).ToList();

            var result = new TrendDataListResponse { Items = dtos, TotalCount = totalCount };
            return Ok(ApiResponse<TrendDataListResponse>.Success(result, "查询成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<TrendDataListResponse>.Error(500, ex.Message));
        }
    }
}