using backend.Entities;

namespace backend.Repositories;

public interface ISchedulePlanRepository
{
    Task<bool> DeleteSchedulePlanAsync(
        int planId,
        CancellationToken cancellationToken);

    Task<int> InsertInitialConditionAsync(
        ScheduleInitialCondition condition,
        CancellationToken cancellationToken);

    Task<int> InsertSchedulePlanAsync(
        SchedulePlan plan,
        CancellationToken cancellationToken);

    Task UpsertHourlyPumpScheduleAsync(
        int planId,
        int hour,
        HourlyPumpSchedule data,
        CancellationToken cancellationToken);

    Task UpsertHourlyAuxiliaryScheduleAsync(
        int planId,
        int hour,
        HourlyAuxiliarySchedule data,
        CancellationToken cancellationToken);

    Task UpsertHourlyEnergyCostAsync(
        int planId,
        int hour,
        HourlyEnergyCost data,
        CancellationToken cancellationToken);

    Task UpsertHourlyTankStatusAsync(
        int planId,
        int hour,
        HourlyTankStatus data,
        CancellationToken cancellationToken);

    Task UpsertBogForecastAsync(
        int planId,
        int hour,
        BogForecast data,
        CancellationToken cancellationToken);

    Task UpsertEquipmentOnOffMatrixAsync(
        int planId,
        int hour,
        EquipmentOnOffMatrix data,
        CancellationToken cancellationToken);

    Task UpsertSchedulePlanDetailAsync(
        int planId,
        SchedulePlanDetail data,
        CancellationToken cancellationToken);

    Task UpsertCompressorScheduleAsync(
        int planId,
        int hour,
        CompressorSchedule data,
        CancellationToken cancellationToken);

    Task UpsertReportDatasetAsync(
        int planId,
        string reportCode,
        string displayName,
        string columnsJson,
        string rowsJson,
        CancellationToken cancellationToken);
}
