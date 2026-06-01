namespace backend.Dtos;

public class DataTreeNodeDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsLeaf { get; set; }
    public string? TableName { get; set; }
    public List<DataTreeNodeDto> Children { get; set; } = new();
}

public class DataTableColumnDto
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsAutoIncrement { get; set; }
    public bool IsForeignKey { get; set; }
    public bool IsPrimaryForeignKey { get; set; }
    public string? ReferencedTableName { get; set; }
    public string? ReferencedColumnName { get; set; }
    public string? ReferencedTableDisplayName { get; set; }
    public string? DefaultValue { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class DataTableDefinitionDto
{
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CategoryKey { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string PrimaryKeyName { get; set; } = string.Empty;
    public List<DataTableColumnDto> Columns { get; set; } = new();
}

public class DataTablePageResponse
{
    public DataTableDefinitionDto Definition { get; set; } = new();
    public List<Dictionary<string, object?>> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class DataTableQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Keyword { get; set; }
}

public class DataTableUpsertDto
{
    public Dictionary<string, System.Text.Json.JsonElement> Data { get; set; } = new();
}
