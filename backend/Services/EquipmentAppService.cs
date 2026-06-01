using backend.Dtos;
using backend.Entities;
using backend.Models.Realtime;
using backend.Repositories;

namespace backend.Services;

public sealed class EquipmentAppService : IEquipmentAppService
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IEquipmentTagBindingService _tagBindingService;
    private readonly IRealtimeCacheService _realtimeCacheService;

    public EquipmentAppService(
        IEquipmentRepository equipmentRepository,
        IEquipmentTagBindingService tagBindingService,
        IRealtimeCacheService realtimeCacheService)
    {
        _equipmentRepository = equipmentRepository;
        _tagBindingService = tagBindingService;
        _realtimeCacheService = realtimeCacheService;
    }

    public async Task<EquipmentCounts> GetCountsAsync(CancellationToken cancellationToken)
    {
        var types = await _equipmentRepository.GetEquipmentTypesAsync(cancellationToken);
        var counts = await _equipmentRepository.GetEquipmentCountsByTypeAsync(cancellationToken);

        var dto = new EquipmentCounts();
        foreach (var type in types)
        {
            counts.TryGetValue(type.TypeId, out var c);

            var typeDto = new EquipmentTypeCounts
            {
                TypeId = type.TypeId,
                TypeCode = type.TypeCode,
                TypeName = type.TypeName,
                ExpectedCount = type.ExpectedCount,
                ActualCount = c.Actual,
                OnlineCount = c.Online,
                OfflineCount = c.Offline
            };

            dto.TotalCount += typeDto.ActualCount;
            dto.EquipmentTypes.Add(typeDto);
        }

        return dto;
    }

    public async Task<EquipmentListResponse> GetEquipmentListAsync(EquipmentQueryDto query, CancellationToken cancellationToken)
    {
        var types = await _equipmentRepository.GetEquipmentTypesAsync(cancellationToken);
        var typeDict = types.ToDictionary(t => t.TypeId, t => t.TypeName);

        var (items, totalCount) = await _equipmentRepository.GetEquipmentListAsync(
            query.TypeId, query.Status, query.SystemCode, query.Keyword,
            query.PageIndex, query.PageSize, cancellationToken);

        var dtos = items.Select(e =>
        {
            var dto = new EquipmentDto
            {
                EquipmentId = e.EquipmentId,
                EquipmentCode = e.EquipmentCode,
                EquipmentName = e.EquipmentName,
                TypeId = e.TypeId,
                ParentEquipmentId = e.ParentEquipmentId,
                SystemCode = e.SystemCode,
                ProcessArea = e.ProcessArea,
                Manufacturer = e.Manufacturer,
                Model = e.Model,
                Location = e.Location,
                Status = e.Status,
                IsControllable = e.IsControllable,
                InstallDate = e.InstallDate,
                CommissionedDate = e.CommissionedDate,
                Remark = e.Remark
            };
            if (typeDict.TryGetValue(e.TypeId, out var typeName))
            {
                dto.TypeName = typeName;
            }

            return dto;
        }).ToList();

        return new EquipmentListResponse { Items = dtos, TotalCount = totalCount };
    }

    public async Task<EquipmentDto?> GetEquipmentByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _equipmentRepository.GetEquipmentByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return null;
        }

        var types = await _equipmentRepository.GetEquipmentTypesAsync(cancellationToken);
        var typeDict = types.ToDictionary(t => t.TypeId, t => t.TypeName);

        var dto = new EquipmentDto
        {
            EquipmentId = entity.EquipmentId,
            EquipmentCode = entity.EquipmentCode,
            EquipmentName = entity.EquipmentName,
            TypeId = entity.TypeId,
            ParentEquipmentId = entity.ParentEquipmentId,
            SystemCode = entity.SystemCode,
            ProcessArea = entity.ProcessArea,
            Manufacturer = entity.Manufacturer,
            Model = entity.Model,
            Location = entity.Location,
            Status = entity.Status,
            IsControllable = entity.IsControllable,
            InstallDate = entity.InstallDate,
            CommissionedDate = entity.CommissionedDate,
            Remark = entity.Remark
        };

        if (typeDict.TryGetValue(entity.TypeId, out var typeName))
        {
            dto.TypeName = typeName;
        }

        return dto;
    }

    public async Task<List<EquipmentTypeDto>> GetEquipmentTypesAsync(CancellationToken cancellationToken)
    {
        var types = await _equipmentRepository.GetEquipmentTypesAsync(cancellationToken);
        return types.Select(t => new EquipmentTypeDto
        {
            TypeId = t.TypeId,
            TypeCode = t.TypeCode,
            TypeName = t.TypeName,
            ExpectedCount = t.ExpectedCount
        }).ToList();
    }

    public async Task<EquipmentMonitoringResponse> GetEquipmentMonitoringAsync(EquipmentMonitoringQueryDto query, CancellationToken cancellationToken)
    {
        var records = await _equipmentRepository.GetEquipmentMonitoringRecordsAsync(query.TypeCode, cancellationToken);

        static string ResolveGroupCode(string typeCode) => typeCode switch
        {
            "SW_PUMP_BIG" => "ORV_CLUSTER",
            "SW_PUMP_SMALL" => "ORV_CLUSTER",
            "ORV" => "ORV_CLUSTER",
            _ => typeCode
        };

        static string ResolveGroupName(string typeCode, string typeName) => typeCode switch
        {
            "LP_PUMP" => "低压泵",
            "HP_PUMP" => "高压泵",
            "SW_PUMP_BIG" => "海水泵及ORV",
            "SW_PUMP_SMALL" => "海水泵及ORV",
            "ORV" => "海水泵及ORV",
            "BOG_COMPRESSOR" => "BOG压缩机",
            "RECONDENSER" => "再冷凝器",
            _ => typeName
        };

        var groups = records
            .GroupBy(item => new
            {
                GroupCode = ResolveGroupCode(item.TypeCode),
                GroupName = ResolveGroupName(item.TypeCode, item.TypeName),
                item.ProcessArea
            })
            .Select(group => new EquipmentMonitoringGroupDto
            {
                GroupCode = group.Key.GroupCode,
                GroupName = group.Key.GroupName,
                ProcessArea = group.Key.ProcessArea,
                Items = group.Select(BuildMonitoringItemDto).ToList()
            })
            .ToList();

        return new EquipmentMonitoringResponse
        {
            Groups = groups
        };
    }

    public async Task<EquipmentMonitoringTrendResponse?> GetEquipmentMonitoringTrendAsync(
        int id,
        DateTime? start,
        DateTime? end,
        CancellationToken cancellationToken)
    {
        var trend = await _equipmentRepository.GetEquipmentMonitoringTrendAsync(id, start, end, cancellationToken);
        if (trend == null)
        {
            return null;
        }

        return new EquipmentMonitoringTrendResponse
        {
            EquipmentId = trend.EquipmentId,
            EquipmentCode = trend.EquipmentCode,
            EquipmentName = trend.EquipmentName,
            Items = trend.Items.Select(item => new EquipmentMonitoringTrendPointDto
            {
                Timestamp = item.Timestamp,
                FlowRate = item.FlowRate,
                Pressure = item.Pressure,
                CurrentPower = item.CurrentPower,
                Temperature = item.Temperature,
                LiquidLevel = item.LiquidLevel,
                Status = item.Status
            }).ToList()
        };
    }

    private EquipmentMonitoringItemDto BuildMonitoringItemDto(EquipmentMonitoringRecord item)
    {
        var binding = _tagBindingService.Resolve(item.EquipmentCode, item.TypeCode);
        var statusSnapshot = GetTag(binding.StatusTagName);

        var status = statusSnapshot != null
            ? ResolveStatus(statusSnapshot.Value, item.Status)
            : item.Status;

        return new EquipmentMonitoringItemDto
        {
            EquipmentId = item.EquipmentId,
            EquipmentCode = item.EquipmentCode,
            EquipmentName = item.EquipmentName,
            TypeCode = item.TypeCode,
            TypeName = item.TypeName,
            ProcessArea = item.ProcessArea,
            Location = item.Location,
            Status = status,
            IsOnline = status.Contains("在线") || status.Contains("运行"),
            FlowRate = ReadDecimal(binding.FlowRateTagName, item.FlowRate),
            Pressure = ReadDecimal(binding.PressureTagName, item.Pressure),
            CurrentPower = ReadDecimal(binding.CurrentPowerTagName, item.CurrentPower),
            Temperature = ReadDecimal(binding.TemperatureTagName, item.Temperature),
            LiquidLevel = ReadDecimal(binding.LiquidLevelTagName, item.LiquidLevel),
            UpdateTime = ResolveUpdateTime(item.UpdateTime, binding),
            Remark = item.Remark,
            TagBindings = new EquipmentMetricTagBindingDto
            {
                FlowRateTagName = binding.FlowRateTagName,
                PressureTagName = binding.PressureTagName,
                CurrentPowerTagName = binding.CurrentPowerTagName,
                TemperatureTagName = binding.TemperatureTagName,
                LiquidLevelTagName = binding.LiquidLevelTagName,
                StatusTagName = binding.StatusTagName
            }
        };
    }

    private DateTime? ResolveUpdateTime(DateTime? fallback, EquipmentMetricTagBinding binding)
    {
        var timestamps = new[]
        {
            GetTag(binding.FlowRateTagName)?.Timestamp,
            GetTag(binding.PressureTagName)?.Timestamp,
            GetTag(binding.CurrentPowerTagName)?.Timestamp,
            GetTag(binding.TemperatureTagName)?.Timestamp,
            GetTag(binding.LiquidLevelTagName)?.Timestamp,
            GetTag(binding.StatusTagName)?.Timestamp
        }.Where(timestamp => timestamp.HasValue)
         .Select(timestamp => timestamp!.Value)
         .ToList();

        return timestamps.Count > 0 ? timestamps.Max() : fallback;
    }

    private decimal? ReadDecimal(string? tagName, decimal? fallback)
    {
        var snapshot = GetTag(tagName);
        return snapshot == null ? fallback : Convert.ToDecimal(snapshot.Value);
    }

    private string ResolveStatus(double value, string fallback)
    {
        if (value >= 1d)
        {
            return "运行";
        }

        if (value <= 0d)
        {
            return "离线";
        }

        return fallback;
    }

    private TagSnapshot? GetTag(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        return _realtimeCacheService.GetTag(tagName);
    }
}
