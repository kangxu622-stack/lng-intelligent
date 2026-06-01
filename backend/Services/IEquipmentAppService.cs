using backend.Dtos;

namespace backend.Services;

public interface IEquipmentAppService
{
    Task<EquipmentCounts> GetCountsAsync(CancellationToken cancellationToken);
    Task<EquipmentListResponse> GetEquipmentListAsync(EquipmentQueryDto query, CancellationToken cancellationToken);
    Task<EquipmentDto?> GetEquipmentByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<EquipmentTypeDto>> GetEquipmentTypesAsync(CancellationToken cancellationToken);
    Task<EquipmentMonitoringResponse> GetEquipmentMonitoringAsync(EquipmentMonitoringQueryDto query, CancellationToken cancellationToken);
    Task<EquipmentMonitoringTrendResponse?> GetEquipmentMonitoringTrendAsync(int id, DateTime? start, DateTime? end, CancellationToken cancellationToken);
}
