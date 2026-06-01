using backend.Entities;

namespace backend.Repositories;

public interface IAlarmRepository
{
    Task<List<AlarmConfig>> GetAlarmConfigsAsync(CancellationToken cancellationToken);
    Task<List<AlarmHistory>> GetActiveAlarmsAsync(CancellationToken cancellationToken);
    Task<(List<AlarmHistory> Items, int TotalCount)> GetAlarmHistoryAsync(
        int? equipmentId, int? tagId, DateTime? startTime, DateTime? endTime,
        int pageIndex, int pageSize, CancellationToken cancellationToken);
}