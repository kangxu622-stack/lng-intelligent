using backend.Entities;

namespace backend.Repositories;

public interface IEquipmentRepository
{
    Task<List<EquipmentType>> GetEquipmentTypesAsync(CancellationToken cancellationToken);

    Task<Dictionary<int, (int Actual, int Online, int Offline)>> GetEquipmentCountsByTypeAsync(CancellationToken cancellationToken);

    Task<(List<Equipment> Items, int TotalCount)> GetEquipmentListAsync(
        int? typeId, string? status, string? systemCode, string? keyword,
        int pageIndex, int pageSize, CancellationToken cancellationToken);

    Task<Equipment?> GetEquipmentByIdAsync(int equipmentId, CancellationToken cancellationToken);
    Task<List<EquipmentMonitoringRecord>> GetEquipmentMonitoringRecordsAsync(string? typeCode, CancellationToken cancellationToken);
    Task<EquipmentMonitoringTrendData?> GetEquipmentMonitoringTrendAsync(int equipmentId, DateTime? start, DateTime? end, CancellationToken cancellationToken);
}
