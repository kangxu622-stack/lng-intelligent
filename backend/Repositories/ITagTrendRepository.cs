using backend.Entities;

namespace backend.Repositories;

public interface ITagTrendRepository
{
    Task InsertBatchAsync(List<TagTrendRecord> records, CancellationToken cancellationToken);
    Task<(List<TagTrendRecord> Items, int TotalCount)> GetHistoryAsync(
        string tagName, DateTime? start, DateTime? end,
        int pageIndex, int pageSize, CancellationToken cancellationToken);
}