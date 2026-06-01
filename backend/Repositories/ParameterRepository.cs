using MySqlConnector;

namespace backend.Repositories;

public sealed class ParameterRepository : IParameterRepository
{
    private readonly string _connectionString;

    public ParameterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async Task<(int TankCount, double TankCapacityM3, double SectionAreaM2, double InitialLiquidLevelM, double TankLevelMax, double TankLevelMin, double TankPressureMax, double TankPressureMin)?>
        GetTankSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                COUNT(*) AS tank_count,
                AVG(tank_capacity_m3) AS tank_capacity_m3,
                AVG(section_area_m2) AS section_area_m2,
                AVG(initial_liquid_level_m) AS initial_liquid_level_m,
                MAX(tank_level_max) AS tank_level_max,
                MIN(tank_level_min) AS tank_level_min,
                MAX(tank_pressure_max) AS tank_pressure_max,
                MIN(tank_pressure_min) AS tank_pressure_min
            FROM tank_detail;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (
            TankCount: GetInt32(reader, "tank_count"),
            TankCapacityM3: GetDouble(reader, "tank_capacity_m3"),
            SectionAreaM2: GetDouble(reader, "section_area_m2"),
            InitialLiquidLevelM: GetDouble(reader, "initial_liquid_level_m"),
            TankLevelMax: GetDouble(reader, "tank_level_max"),
            TankLevelMin: GetDouble(reader, "tank_level_min"),
            TankPressureMax: GetDouble(reader, "tank_pressure_max"),
            TankPressureMin: GetDouble(reader, "tank_pressure_min"));
    }

    public async Task<(int PumpCount, double MinFlowM3h, double MaxFlowM3h, double RatedPowerKw, double PqA, double PqB, double PqC, double TargetPressureMpa)?>
        GetLowPressurePumpSummaryAsync(CancellationToken cancellationToken)
    {
        return await GetPumpSummaryAsync("low_pressure_pump_detail", cancellationToken);
    }

    public async Task<(int PumpCount, double MinFlowM3h, double MaxFlowM3h, double RatedPowerKw, double PqA, double PqB, double PqC, double TargetPressureMpa)?>
        GetHighPressurePumpSummaryAsync(CancellationToken cancellationToken)
    {
        return await GetPumpSummaryAsync("high_pressure_pump_detail", cancellationToken);
    }

    public async Task<List<(int Count, double RatedFlowM3h, double RatedPowerKw)>> GetSeawaterPumpGroupsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                COUNT(*) AS pump_count,
                AVG(rated_flow_m3h) AS rated_flow_m3h,
                AVG(rated_power_kw) AS rated_power_kw
            FROM seawater_pump_detail
            GROUP BY pump_class;
            """;

        var result = new List<(int Count, double RatedFlowM3h, double RatedPowerKw)>();

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add((
                Count: GetInt32(reader, "pump_count"),
                RatedFlowM3h: GetDouble(reader, "rated_flow_m3h"),
                RatedPowerKw: GetDouble(reader, "rated_power_kw")));
        }

        return result;
    }

    public async Task<(int OrvCount, double MaxFlowM3h)?> GetOrvSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                COUNT(*) AS orv_count,
                AVG(max_flow_m3h) AS max_flow_m3h
            FROM orv_detail;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (
            OrvCount: GetInt32(reader, "orv_count"),
            MaxFlowM3h: GetDouble(reader, "max_flow_m3h"));
    }

    public async Task<(int CompressorCount, double RatedCapacityKgph, string? PowerLevelsJson, string? LoadLevelsJson)?> GetBogCompressorSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                COUNT(*) AS compressor_count,
                AVG(rated_capacity_kgph) AS rated_capacity_kgph,
                MAX(power_level_json) AS power_level_json,
                MAX(load_level_json) AS load_level_json
            FROM bog_compressor_detail;
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (
            CompressorCount: GetInt32(reader, "compressor_count"),
            RatedCapacityKgph: GetDouble(reader, "rated_capacity_kgph"),
            PowerLevelsJson: GetNullableString(reader, "power_level_json"),
            LoadLevelsJson: GetNullableString(reader, "load_level_json"));
    }

    public async Task<List<(string PropertyName, double PropertyValue, string? Unit, string? Category, string? MediumType, string? Description)>> GetProcessMediumPropertiesAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT property_name, property_value, unit, property_category, medium_type, description
            FROM process_medium_property
            WHERE is_active = 1;
            """;

        var result = new List<(string PropertyName, double PropertyValue, string? Unit, string? Category, string? MediumType, string? Description)>();
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add((
                PropertyName: reader.GetString("property_name"),
                PropertyValue: reader.GetDouble("property_value"),
                Unit: GetNullableString(reader, "unit"),
                Category: GetNullableString(reader, "property_category"),
                MediumType: GetNullableString(reader, "medium_type"),
                Description: GetNullableString(reader, "description")));
        }

        return result;
    }

    public async Task<List<(string Name, string Phase, double AreaM2, double LevelMinM, double PumpStartLevelM, double LevelMaxM, double LevelInitM)>> GetTankMasterDataAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                COALESCE(NULLIF(e.algorithm_name, ''), e.equipment_name) AS tank_name,
                COALESCE(NULLIF(e.project_phase, ''), '一期') AS project_phase,
                td.section_area_m2,
                td.tank_level_min,
                td.pump_start_level_m,
                td.tank_level_max,
                td.initial_liquid_level_m
            FROM equipment e
            INNER JOIN tank_detail td ON td.equipment_id = e.equipment_id
            ORDER BY e.equipment_id;
            """;

        var result = new List<(string Name, string Phase, double AreaM2, double LevelMinM, double PumpStartLevelM, double LevelMaxM, double LevelInitM)>();
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add((
                Name: reader.GetString("tank_name"),
                Phase: reader.GetString("project_phase"),
                AreaM2: GetDouble(reader, "section_area_m2"),
                LevelMinM: GetDouble(reader, "tank_level_min"),
                PumpStartLevelM: GetDouble(reader, "pump_start_level_m"),
                LevelMaxM: GetDouble(reader, "tank_level_max"),
                LevelInitM: GetDouble(reader, "initial_liquid_level_m")));
        }

        return result;
    }

    public async Task<List<(string Name, string Category, string? Line, double RatedPowerKw, double ReactiveKvar, double MinFlowM3h, double MaxFlowM3h, string? TankName, double CapacityKgph, string Phase)>> GetDeviceMasterDataAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                COALESCE(NULLIF(e.algorithm_name, ''), e.equipment_name) AS device_name,
                CASE
                    WHEN et.type_code = 'LP_PUMP' THEN 'LP'
                    WHEN et.type_code = 'HP_PUMP' THEN 'HP'
                    WHEN et.type_code IN ('SW_PUMP_BIG', 'SW_PUMP_SMALL') THEN 'SW'
                    WHEN et.type_code = 'BOG_COMPRESSOR' THEN 'COMP'
                    WHEN et.type_code = 'ORV' THEN 'ORV'
                    ELSE et.type_code
                END AS category,
                e.power_line,
                COALESCE(lpd.rated_power_kw, hpd.rated_power_kw, spd.rated_power_kw, bcd.rated_power_kw, 0) AS rated_power_kw,
                COALESCE(e.reactive_power_kvar, 0) AS reactive_power_kvar,
                COALESCE(lpd.min_flow_m3h, hpd.min_flow_m3h, 0) AS min_flow_m3h,
                COALESCE(lpd.max_flow_m3h, hpd.max_flow_m3h, spd.max_flow_m3h, od.max_flow_m3h, 0) AS max_flow_m3h,
                parent.algorithm_name AS tank_name,
                COALESCE(bcd.rated_capacity_kgph, 0) AS capacity_kgph,
                COALESCE(NULLIF(e.project_phase, ''), '一期') AS project_phase
            FROM equipment e
            INNER JOIN equipment_type et ON et.type_id = e.type_id
            LEFT JOIN equipment parent ON parent.equipment_id = e.parent_equipment_id
            LEFT JOIN low_pressure_pump_detail lpd ON lpd.equipment_id = e.equipment_id
            LEFT JOIN high_pressure_pump_detail hpd ON hpd.equipment_id = e.equipment_id
            LEFT JOIN seawater_pump_detail spd ON spd.equipment_id = e.equipment_id
            LEFT JOIN bog_compressor_detail bcd ON bcd.equipment_id = e.equipment_id
            LEFT JOIN orv_detail od ON od.equipment_id = e.equipment_id
            WHERE et.type_code IN ('LP_PUMP', 'HP_PUMP', 'SW_PUMP_BIG', 'SW_PUMP_SMALL', 'BOG_COMPRESSOR', 'ORV')
            ORDER BY e.equipment_id;
            """;

        var result = new List<(string Name, string Category, string? Line, double RatedPowerKw, double ReactiveKvar, double MinFlowM3h, double MaxFlowM3h, string? TankName, double CapacityKgph, string Phase)>();
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add((
                Name: reader.GetString("device_name"),
                Category: reader.GetString("category"),
                Line: GetNullableString(reader, "power_line"),
                RatedPowerKw: GetDouble(reader, "rated_power_kw"),
                ReactiveKvar: GetDouble(reader, "reactive_power_kvar"),
                MinFlowM3h: GetDouble(reader, "min_flow_m3h"),
                MaxFlowM3h: GetDouble(reader, "max_flow_m3h"),
                TankName: GetNullableString(reader, "tank_name"),
                CapacityKgph: GetDouble(reader, "capacity_kgph"),
                Phase: reader.GetString("project_phase")));
        }

        return result;
    }

    private async Task<(int PumpCount, double MinFlowM3h, double MaxFlowM3h, double RatedPowerKw, double PqA, double PqB, double PqC, double TargetPressureMpa)?>
        GetPumpSummaryAsync(string tableName, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"""
            SELECT
                COUNT(*) AS pump_count,
                AVG(min_flow_m3h) AS min_flow_m3h,
                AVG(max_flow_m3h) AS max_flow_m3h,
                AVG(rated_power_kw) AS rated_power_kw,
                AVG(pq_curve_a) AS pq_curve_a,
                AVG(pq_curve_b) AS pq_curve_b,
                AVG(pq_curve_c) AS pq_curve_c,
                AVG(target_pressure_mpa) AS target_pressure_mpa
            FROM {tableName};
            """;

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (
            PumpCount: GetInt32(reader, "pump_count"),
            MinFlowM3h: GetDouble(reader, "min_flow_m3h"),
            MaxFlowM3h: GetDouble(reader, "max_flow_m3h"),
            RatedPowerKw: GetDouble(reader, "rated_power_kw"),
            PqA: GetDouble(reader, "pq_curve_a"),
            PqB: GetDouble(reader, "pq_curve_b"),
            PqC: GetDouble(reader, "pq_curve_c"),
            TargetPressureMpa: GetDouble(reader, "target_pressure_mpa"));
    }

    private static int GetInt32(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static double GetDouble(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return 0.0;
        }

        return Convert.ToDouble(reader.GetValue(ordinal));
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetString(ordinal);
    }
}
