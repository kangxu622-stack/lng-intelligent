using backend.Models.Realtime;

namespace backend.Services;

public interface IEquipmentTagBindingService
{
    EquipmentMetricTagBinding Resolve(string equipmentCode, string typeCode);
}
