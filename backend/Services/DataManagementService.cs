using System.Globalization;
using System.Text.Json;
using backend.Dtos;
using MySqlConnector;

namespace backend.Services;

public sealed class DataManagementService : IDataManagementService
{
    private sealed record TableConfig(string TableName, string DisplayName, string CategoryKey, string CategoryName);
    private sealed record ColumnMeta(
        string Name,
        string ColumnType,
        string DataType,
        bool IsNullable,
        string ColumnKey,
        string? DefaultValue,
        string Extra,
        string Comment,
        string? ReferencedTableName,
        string? ReferencedColumnName
    );

    private static readonly List<TableConfig> AllowedTables =
    [
        new("equipment_type", "设备类型", "equipment-base", "设备基础"),
        new("equipment", "设备台账", "equipment-base", "设备基础"),
        new("tank_detail", "储罐静态参数", "equipment-detail", "设备明细"),
        new("low_pressure_pump_detail", "低压泵静态参数", "equipment-detail", "设备明细"),
        new("high_pressure_pump_detail", "高压泵静态参数", "equipment-detail", "设备明细"),
        new("seawater_pump_detail", "海水泵静态参数", "equipment-detail", "设备明细"),
        new("orv_detail", "ORV静态参数", "equipment-detail", "设备明细"),
        new("bog_compressor_detail", "BOG压缩机静态参数", "equipment-detail", "设备明细"),
        new("recondenser_detail", "再冷凝器静态参数", "equipment-detail", "设备明细"),
        new("ship_detail", "LNG船舶静态参数", "equipment-detail", "设备明细"),
        new("berth_detail", "泊位静态参数", "equipment-detail", "设备明细"),
        new("unloading_arm_detail", "卸料臂静态参数", "equipment-detail", "设备明细"),
        new("process_medium_property", "介质物性参数", "process-base", "工艺基础"),
        new("valve_info", "阀门静态信息", "process-base", "工艺基础"),
        new("startup_shutdown_rule", "启停规则", "process-base", "工艺基础"),
        new("process_node", "流程节点", "process-network", "流程结构"),
        new("process_pipeline", "流程管线", "process-network", "流程结构")
    ];

    private readonly string _connectionString;
    private readonly string _databaseName;

    public DataManagementService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        var builder = new MySqlConnectionStringBuilder(_connectionString);
        _databaseName = builder.Database;
    }

    public Task<List<DataTreeNodeDto>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var tree = AllowedTables
            .GroupBy(item => new { item.CategoryKey, item.CategoryName })
            .Select(group => new DataTreeNodeDto
            {
                Key = group.Key.CategoryKey,
                Label = group.Key.CategoryName,
                IsLeaf = false,
                Children = group.Select(item => new DataTreeNodeDto
                {
                    Key = item.TableName,
                    Label = item.DisplayName,
                    IsLeaf = true,
                    TableName = item.TableName
                }).ToList()
            })
            .ToList();

        return Task.FromResult(tree);
    }

    public async Task<DataTablePageResponse> GetTablePageAsync(string tableName, DataTableQueryDto query, CancellationToken cancellationToken)
    {
        var config = GetConfig(tableName);
        var columns = await GetColumnsAsync(tableName, cancellationToken);
        var primaryKey = columns.FirstOrDefault(col => col.ColumnKey == "PRI")?.Name ?? columns.First().Name;
        var keywordColumns = columns
            .Where(col => col.DataType.Contains("char", StringComparison.OrdinalIgnoreCase)
                || col.DataType.Contains("text", StringComparison.OrdinalIgnoreCase))
            .ToList();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereParts = new List<string>();
        var countParameters = new List<MySqlParameter>();

        if (!string.IsNullOrWhiteSpace(query.Keyword) && keywordColumns.Count > 0)
        {
            var likeParts = new List<string>();
            for (var i = 0; i < keywordColumns.Count; i++)
            {
                var paramName = $"@kw{i}";
                likeParts.Add($"`{keywordColumns[i].Name}` LIKE {paramName}");
                countParameters.Add(new MySqlParameter(paramName, $"%{query.Keyword}%"));
            }

            whereParts.Add($"({string.Join(" OR ", likeParts)})");
        }

        var whereSql = whereParts.Count > 0 ? $" WHERE {string.Join(" AND ", whereParts)}" : string.Empty;

        var countSql = $"SELECT COUNT(*) FROM `{tableName}`{whereSql};";
        await using var countCmd = new MySqlCommand(countSql, connection);
        countCmd.Parameters.AddRange(countParameters.ToArray());
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        var pageIndex = query.PageIndex <= 0 ? 1 : query.PageIndex;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var offset = (pageIndex - 1) * pageSize;

        var selectSql = $"SELECT * FROM `{tableName}`{whereSql} ORDER BY `{primaryKey}` LIMIT @pageSize OFFSET @offset;";
        await using var selectCmd = new MySqlCommand(selectSql, connection);
        selectCmd.Parameters.AddRange(countParameters.Select(p => new MySqlParameter(p.ParameterName, p.Value)).ToArray());
        selectCmd.Parameters.AddWithValue("@pageSize", pageSize);
        selectCmd.Parameters.AddWithValue("@offset", offset);

        var items = new List<Dictionary<string, object?>>();
        await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadRow(reader));
        }

        return new DataTablePageResponse
        {
            Definition = new DataTableDefinitionDto
            {
                TableName = config.TableName,
                DisplayName = config.DisplayName,
                CategoryKey = config.CategoryKey,
                CategoryName = config.CategoryName,
                PrimaryKeyName = primaryKey,
                Columns = columns.Select(col => new DataTableColumnDto
                {
                    Name = col.Name,
                    Label = string.IsNullOrWhiteSpace(col.Comment) ? col.Name : col.Comment,
                    DataType = col.ColumnType,
                    IsNullable = col.IsNullable,
                    IsPrimaryKey = col.ColumnKey == "PRI",
                    IsAutoIncrement = col.Extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                    IsForeignKey = !string.IsNullOrWhiteSpace(col.ReferencedTableName),
                    IsPrimaryForeignKey = col.ColumnKey == "PRI" && !string.IsNullOrWhiteSpace(col.ReferencedTableName),
                    ReferencedTableName = col.ReferencedTableName,
                    ReferencedColumnName = col.ReferencedColumnName,
                    ReferencedTableDisplayName = GetDisplayNameOrNull(col.ReferencedTableName),
                    DefaultValue = col.DefaultValue,
                    Comment = col.Comment
                }).ToList()
            },
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<Dictionary<string, object?>?> GetRowAsync(string tableName, string id, CancellationToken cancellationToken)
    {
        var columns = await GetColumnsAsync(tableName, cancellationToken);
        var primaryKey = columns.FirstOrDefault(col => col.ColumnKey == "PRI")
            ?? throw new InvalidOperationException($"Table {tableName} has no primary key.");

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"SELECT * FROM `{tableName}` WHERE `{primaryKey.Name}` = @id LIMIT 1;";
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", ConvertPrimaryKeyValue(id, primaryKey));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return ReadRow(reader);
        }

        return null;
    }

    public async Task<Dictionary<string, object?>> CreateRowAsync(string tableName, DataTableUpsertDto input, CancellationToken cancellationToken)
    {
        var columns = await GetColumnsAsync(tableName, cancellationToken);
        var insertColumns = columns
            .Where(col => !col.Extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase))
            .Where(col => input.Data.ContainsKey(col.Name))
            .ToList();

        if (insertColumns.Count == 0)
        {
            throw new InvalidOperationException("No writable fields were provided.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var columnSql = string.Join(", ", insertColumns.Select(col => $"`{col.Name}`"));
        var valueSql = string.Join(", ", insertColumns.Select((col, index) => $"@p{index}"));
        var sql = $"INSERT INTO `{tableName}` ({columnSql}) VALUES ({valueSql});";

        await using var cmd = new MySqlCommand(sql, connection);
        for (var i = 0; i < insertColumns.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@p{i}", ConvertValue(input.Data[insertColumns[i].Name], insertColumns[i]));
        }

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Number is 1451 or 1452)
        {
            throw CreateFriendlyConstraintException(tableName, ex);
        }

        var primaryKey = columns.FirstOrDefault(col => col.ColumnKey == "PRI");
        if (primaryKey == null)
        {
            throw new InvalidOperationException($"Table {tableName} has no primary key.");
        }

        var idValue = primaryKey.Extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase)
            ? cmd.LastInsertedId.ToString()
            : input.Data[primaryKey.Name].ToString() ?? string.Empty;

        return await GetRowAsync(tableName, idValue, cancellationToken)
            ?? throw new InvalidOperationException("Created row could not be loaded.");
    }

    public async Task<Dictionary<string, object?>?> UpdateRowAsync(string tableName, string id, DataTableUpsertDto input, CancellationToken cancellationToken)
    {
        var columns = await GetColumnsAsync(tableName, cancellationToken);
        var primaryKey = columns.FirstOrDefault(col => col.ColumnKey == "PRI")
            ?? throw new InvalidOperationException($"Table {tableName} has no primary key.");

        var updateColumns = columns
            .Where(col => col.Name != primaryKey.Name)
            .Where(col => input.Data.ContainsKey(col.Name))
            .ToList();

        if (updateColumns.Count == 0)
        {
            throw new InvalidOperationException("No updatable fields were provided.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var setSql = string.Join(", ", updateColumns.Select((col, index) => $"`{col.Name}` = @p{index}"));
        var sql = $"UPDATE `{tableName}` SET {setSql} WHERE `{primaryKey.Name}` = @id;";

        await using var cmd = new MySqlCommand(sql, connection);
        for (var i = 0; i < updateColumns.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@p{i}", ConvertValue(input.Data[updateColumns[i].Name], updateColumns[i]));
        }
        cmd.Parameters.AddWithValue("@id", ConvertPrimaryKeyValue(id, primaryKey));

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Number is 1451 or 1452)
        {
            throw CreateFriendlyConstraintException(tableName, ex);
        }

        return await GetRowAsync(tableName, id, cancellationToken);
    }

    public async Task<bool> DeleteRowAsync(string tableName, string id, CancellationToken cancellationToken)
    {
        var columns = await GetColumnsAsync(tableName, cancellationToken);
        var primaryKey = columns.FirstOrDefault(col => col.ColumnKey == "PRI")
            ?? throw new InvalidOperationException($"Table {tableName} has no primary key.");

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"DELETE FROM `{tableName}` WHERE `{primaryKey.Name}` = @id;";
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", ConvertPrimaryKeyValue(id, primaryKey));

        try
        {
            return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (MySqlException ex) when (ex.Number is 1451 or 1452)
        {
            throw CreateFriendlyConstraintException(tableName, ex);
        }
    }

    private TableConfig GetConfig(string tableName)
    {
        return AllowedTables.FirstOrDefault(item => item.TableName == tableName)
            ?? throw new InvalidOperationException($"Table {tableName} is not allowed.");
    }

    private async Task<List<ColumnMeta>> GetColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        _ = GetConfig(tableName);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT
                c.COLUMN_NAME,
                c.COLUMN_TYPE,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.COLUMN_KEY,
                c.COLUMN_DEFAULT,
                c.EXTRA,
                c.COLUMN_COMMENT,
                kcu.REFERENCED_TABLE_NAME,
                kcu.REFERENCED_COLUMN_NAME
            FROM information_schema.COLUMNS c
            LEFT JOIN information_schema.KEY_COLUMN_USAGE kcu
                ON c.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                AND c.TABLE_NAME = kcu.TABLE_NAME
                AND c.COLUMN_NAME = kcu.COLUMN_NAME
                AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
            WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
            ORDER BY c.ORDINAL_POSITION;";

        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", _databaseName);
        cmd.Parameters.AddWithValue("@table", tableName);

        var result = new List<ColumnMeta>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ColumnMeta(
                reader.GetString("COLUMN_NAME"),
                reader.GetString("COLUMN_TYPE"),
                reader.GetString("DATA_TYPE"),
                reader.GetString("IS_NULLABLE").Equals("YES", StringComparison.OrdinalIgnoreCase),
                reader.IsDBNull(reader.GetOrdinal("COLUMN_KEY")) ? string.Empty : reader.GetString("COLUMN_KEY"),
                reader.IsDBNull(reader.GetOrdinal("COLUMN_DEFAULT")) ? null : reader.GetString("COLUMN_DEFAULT"),
                reader.IsDBNull(reader.GetOrdinal("EXTRA")) ? string.Empty : reader.GetString("EXTRA"),
                reader.IsDBNull(reader.GetOrdinal("COLUMN_COMMENT")) ? string.Empty : reader.GetString("COLUMN_COMMENT"),
                reader.IsDBNull(reader.GetOrdinal("REFERENCED_TABLE_NAME")) ? null : reader.GetString("REFERENCED_TABLE_NAME"),
                reader.IsDBNull(reader.GetOrdinal("REFERENCED_COLUMN_NAME")) ? null : reader.GetString("REFERENCED_COLUMN_NAME")
            ));
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException($"Table {tableName} does not exist.");
        }

        return result;
    }

    private static Dictionary<string, object?> ReadRow(MySqlDataReader reader)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        return row;
    }

    private static object? ConvertPrimaryKeyValue(string id, ColumnMeta column)
    {
        return ConvertStringToValue(id, column);
    }

    private static object? ConvertValue(JsonElement element, ColumnMeta column)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return DBNull.Value;
        }

        return column.DataType.ToLowerInvariant() switch
        {
            "tinyint" when column.ColumnType.Equals("tinyint(1)", StringComparison.OrdinalIgnoreCase)
                => element.ValueKind == JsonValueKind.True || (element.ValueKind == JsonValueKind.Number && element.GetInt32() != 0),
            "bit" => element.ValueKind == JsonValueKind.True || (element.ValueKind == JsonValueKind.Number && element.GetInt32() != 0),
            "int" or "integer" or "mediumint" => element.GetInt32(),
            "bigint" => element.GetInt64(),
            "smallint" => element.GetInt16(),
            "decimal" or "numeric" => element.GetDecimal(),
            "double" => element.GetDouble(),
            "float" => element.GetSingle(),
            "datetime" or "timestamp" or "date" => ParseDateTimeElement(element),
            "json" => element.GetRawText(),
            _ => element.ToString()
        };
    }

    private static object? ConvertStringToValue(string value, ColumnMeta column)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DBNull.Value;
        }

        return column.DataType.ToLowerInvariant() switch
        {
            "tinyint" when column.ColumnType.Equals("tinyint(1)", StringComparison.OrdinalIgnoreCase) => value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase),
            "bit" => value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase),
            "int" or "integer" or "mediumint" => int.Parse(value, CultureInfo.InvariantCulture),
            "bigint" => long.Parse(value, CultureInfo.InvariantCulture),
            "smallint" => short.Parse(value, CultureInfo.InvariantCulture),
            "decimal" or "numeric" => decimal.Parse(value, CultureInfo.InvariantCulture),
            "double" => double.Parse(value, CultureInfo.InvariantCulture),
            "float" => float.Parse(value, CultureInfo.InvariantCulture),
            "datetime" or "timestamp" or "date" => DateTime.Parse(value, CultureInfo.InvariantCulture),
            _ => value
        };
    }

    private static DateTime ParseDateTimeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => DateTime.Parse(element.GetString() ?? string.Empty, CultureInfo.InvariantCulture),
            _ => DateTime.Parse(element.ToString(), CultureInfo.InvariantCulture)
        };
    }

    private string? GetDisplayNameOrNull(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return null;
        }

        return AllowedTables.FirstOrDefault(item => item.TableName == tableName)?.DisplayName;
    }

    private Exception CreateFriendlyConstraintException(string tableName, MySqlException ex)
    {
        var sourceConfig = GetConfig(tableName);
        var referenceTable = ResolveReferencedTableFromMessage(ex.Message);
        if (!string.IsNullOrWhiteSpace(referenceTable))
        {
            var referencedConfig = AllowedTables.FirstOrDefault(item => item.TableName == referenceTable);
            if (ex.Number == 1452)
            {
                var referencedName = referencedConfig?.DisplayName ?? referenceTable;
                return new InvalidOperationException($"请先添加“{referencedName}”数据，再维护“{sourceConfig.DisplayName}”。", ex);
            }

            if (ex.Number == 1451)
            {
                var referencedName = referencedConfig?.DisplayName ?? referenceTable;
                return new InvalidOperationException($"“{sourceConfig.DisplayName}”已被“{referencedName}”使用，暂时不能删除。", ex);
            }
        }

        return ex.Number switch
        {
            1452 => new InvalidOperationException($"当前数据依赖其他基础表，请先补充关联数据后再保存“{sourceConfig.DisplayName}”。", ex),
            1451 => new InvalidOperationException($"“{sourceConfig.DisplayName}”已被其他数据引用，暂时不能删除。", ex),
            _ => ex
        };
    }

    private static string? ResolveReferencedTableFromMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var referencesIndex = message.IndexOf("REFERENCES `", StringComparison.OrdinalIgnoreCase);
        if (referencesIndex >= 0)
        {
            var tableStart = referencesIndex + "REFERENCES `".Length;
            var tableEnd = message.IndexOf('`', tableStart);
            if (tableEnd > tableStart)
            {
                return message[tableStart..tableEnd];
            }
        }

        return null;
    }
}
