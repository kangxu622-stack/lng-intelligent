namespace backend.Repositories;

public interface IParameterRepository
{
    Task<(int TankCount, double TankCapacityM3, double SectionAreaM2, double InitialLiquidLevelM, double TankLevelMax, double TankLevelMin, double TankPressureMax, double TankPressureMin)?>
        GetTankSummaryAsync(CancellationToken cancellationToken);

    Task<(int PumpCount, double MinFlowM3h, double MaxFlowM3h, double RatedPowerKw, double PqA, double PqB, double PqC, double TargetPressureMpa)?>
        GetLowPressurePumpSummaryAsync(CancellationToken cancellationToken);

    Task<(int PumpCount, double MinFlowM3h, double MaxFlowM3h, double RatedPowerKw, double PqA, double PqB, double PqC, double TargetPressureMpa)?>
        GetHighPressurePumpSummaryAsync(CancellationToken cancellationToken);

    Task<List<(int Count, double RatedFlowM3h, double RatedPowerKw)>> GetSeawaterPumpGroupsAsync(CancellationToken cancellationToken);

    Task<(int OrvCount, double MaxFlowM3h)?> GetOrvSummaryAsync(CancellationToken cancellationToken);

    Task<(int CompressorCount, double RatedCapacityKgph, string? PowerLevelsJson, string? LoadLevelsJson)?> GetBogCompressorSummaryAsync(CancellationToken cancellationToken);

    Task<List<(string PropertyName, double PropertyValue, string? Unit, string? Category, string? MediumType, string? Description)>> GetProcessMediumPropertiesAsync(CancellationToken cancellationToken);

    Task<List<(string Name, string Phase, double AreaM2, double LevelMinM, double PumpStartLevelM, double LevelMaxM, double LevelInitM)>> GetTankMasterDataAsync(CancellationToken cancellationToken);

    Task<List<(string Name, string Category, string? Line, double RatedPowerKw, double ReactiveKvar, double MinFlowM3h, double MaxFlowM3h, string? TankName, double CapacityKgph, string Phase)>> GetDeviceMasterDataAsync(CancellationToken cancellationToken);
}
