using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/data-management")]
public class DataManagementController : ControllerBase
{
    private readonly IDataManagementService _service;

    public DataManagementController(IDataManagementService service)
    {
        _service = service;
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse<List<DataTreeNodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetTreeAsync(cancellationToken);
            return Ok(ApiResponse<List<DataTreeNodeDto>>.Success(result, "静态数据树查询成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<List<DataTreeNodeDto>>.Error(500, $"静态数据树查询失败: {ex.Message}"));
        }
    }

    [HttpGet("tables/{tableName}")]
    [ProducesResponseType(typeof(ApiResponse<DataTablePageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTablePage(string tableName, [FromQuery] DataTableQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetTablePageAsync(tableName, query, cancellationToken);
            return Ok(ApiResponse<DataTablePageResponse>.Success(result, "静态数据查询成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<DataTablePageResponse>.Error(500, $"静态数据查询失败: {ex.Message}"));
        }
    }

    [HttpGet("tables/{tableName}/{id}")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRow(string tableName, string id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetRowAsync(tableName, id, cancellationToken);
            if (result == null)
            {
                return Ok(ApiResponse<Dictionary<string, object?>>.Error(404, "数据不存在"));
            }

            return Ok(ApiResponse<Dictionary<string, object?>>.Success(result, "静态数据详情查询成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<Dictionary<string, object?>>.Error(500, $"静态数据详情查询失败: {ex.Message}"));
        }
    }

    [HttpPost("tables/{tableName}")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(string tableName, [FromBody] DataTableUpsertDto input, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateRowAsync(tableName, input, cancellationToken);
            return Ok(ApiResponse<Dictionary<string, object?>>.Success(result, "静态数据新增成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<Dictionary<string, object?>>.Error(500, $"静态数据新增失败: {ex.Message}"));
        }
    }

    [HttpPut("tables/{tableName}/{id}")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string tableName, string id, [FromBody] DataTableUpsertDto input, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateRowAsync(tableName, id, input, cancellationToken);
            if (result == null)
            {
                return Ok(ApiResponse<Dictionary<string, object?>>.Error(404, "数据不存在"));
            }

            return Ok(ApiResponse<Dictionary<string, object?>>.Success(result, "静态数据更新成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<Dictionary<string, object?>>.Error(500, $"静态数据更新失败: {ex.Message}"));
        }
    }

    [HttpDelete("tables/{tableName}/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string tableName, string id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _service.DeleteRowAsync(tableName, id, cancellationToken);
            if (!success)
            {
                return Ok(ApiResponse.Error(404, "数据不存在"));
            }

            return Ok(ApiResponse.Success(message: "静态数据删除成功"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse.Error(500, $"静态数据删除失败: {ex.Message}"));
        }
    }
}
