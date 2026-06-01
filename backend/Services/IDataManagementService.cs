using backend.Dtos;

namespace backend.Services;

public interface IDataManagementService
{
    Task<List<DataTreeNodeDto>> GetTreeAsync(CancellationToken cancellationToken);
    Task<DataTablePageResponse> GetTablePageAsync(string tableName, DataTableQueryDto query, CancellationToken cancellationToken);
    Task<Dictionary<string, object?>?> GetRowAsync(string tableName, string id, CancellationToken cancellationToken);
    Task<Dictionary<string, object?>> CreateRowAsync(string tableName, DataTableUpsertDto input, CancellationToken cancellationToken);
    Task<Dictionary<string, object?>?> UpdateRowAsync(string tableName, string id, DataTableUpsertDto input, CancellationToken cancellationToken);
    Task<bool> DeleteRowAsync(string tableName, string id, CancellationToken cancellationToken);
}
