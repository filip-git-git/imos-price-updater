namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents information about a database table's columns for mapping purposes.
/// </summary>
public sealed class TableMappingInfo
{
    /// <summary>
    ///     Gets or sets the name of the SQL table.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the schema name (e.g., "dbo").
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    ///     Gets or sets the fully qualified table name (schema.table).
    /// </summary>
    public string FullyQualifiedName => $"{SchemaName}.{TableName}";

    /// <summary>
    ///     Gets or sets the collection of columns in the table.
    /// </summary>
    public List<ColumnInfo> Columns { get; set; } = new();

    /// <summary>
    ///     Validates that this mapping info has required data.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(TableName)
               && !string.IsNullOrWhiteSpace(SchemaName)
               && Columns.Count > 0;
    }
}

/// <summary>
///     Represents information about a database column.
/// </summary>
public sealed class ColumnInfo
{
    /// <summary>
    ///     Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the SQL data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the column allows null values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    ///     Gets a value indicating whether this column is suitable for searching.
    /// </summary>
    /// <remarks>
    ///     A column is considered suitable for searching if it's a string type
    ///     (varchar, nvarchar, char, nchar) or a numeric type that could be used as an ID.
    /// </remarks>
    public bool IsSearchable => IsStringType || IsNumericType;

    /// <summary>
    ///     Gets a value indicating whether this is a string type column.
    /// </summary>
    public bool IsStringType => DataType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase)
                                || DataType.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase)
                                || DataType.StartsWith("char", StringComparison.OrdinalIgnoreCase)
                                || DataType.StartsWith("nchar", StringComparison.OrdinalIgnoreCase)
                                || DataType.StartsWith("text", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets a value indicating whether this is a numeric type column.
    /// </summary>
    public bool IsNumericType => DataType.StartsWith("int", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("bigint", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("smallint", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("tinyint", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("decimal", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("numeric", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("money", StringComparison.OrdinalIgnoreCase)
                                  || DataType.StartsWith("smallmoney", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets a value indicating whether this column is suitable for price storage.
    /// </summary>
    /// <remarks>
    ///     A column is considered suitable for price if it's a decimal, numeric, money, or float type.
    /// </remarks>
    public bool IsPriceType => DataType.StartsWith("decimal", StringComparison.OrdinalIgnoreCase)
                               || DataType.StartsWith("numeric", StringComparison.OrdinalIgnoreCase)
                               || DataType.StartsWith("money", StringComparison.OrdinalIgnoreCase)
                               || DataType.StartsWith("smallmoney", StringComparison.OrdinalIgnoreCase)
                               || DataType.StartsWith("float", StringComparison.OrdinalIgnoreCase)
                               || DataType.StartsWith("real", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
///     Represents validation result for column mapping.
/// </summary>
public sealed class ColumnMappingValidationResult
{
    /// <summary>
    ///     Gets a value indicating whether the mapping is valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    ///     Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>
    ///     Gets a value indicating whether the search column is valid.
    /// </summary>
    public bool IsSearchColumnValid { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the price column is valid.
    /// </summary>
    public bool IsPriceColumnValid { get; private set; }

    private ColumnMappingValidationResult()
    {
    }

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    /// <returns>A valid result.</returns>
    public static ColumnMappingValidationResult Success()
    {
        return new ColumnMappingValidationResult
        {
            IsValid = true,
            IsSearchColumnValid = true,
            IsPriceColumnValid = true
        };
    }

    /// <summary>
    ///     Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The collection of errors.</param>
    /// <param name="isSearchColumnValid">Whether the search column is valid.</param>
    /// <param name="isPriceColumnValid">Whether the price column is valid.</param>
    /// <returns>A failed result.</returns>
    public static ColumnMappingValidationResult Failure(
        IEnumerable<string> errors,
        bool isSearchColumnValid = false,
        bool isPriceColumnValid = false)
    {
        return new ColumnMappingValidationResult
        {
            IsValid = false,
            Errors = errors.ToList().AsReadOnly(),
            IsSearchColumnValid = isSearchColumnValid,
            IsPriceColumnValid = isPriceColumnValid
        };
    }
}

