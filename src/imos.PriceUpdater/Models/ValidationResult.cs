namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the type of validation error.
/// </summary>
public enum CsvValidationErrorType
{
    /// <summary>
    ///     A required column is missing from the CSV.
    /// </summary>
    MissingColumn,

    /// <summary>
    ///     The data type of a value is invalid.
    /// </summary>
    InvalidDataType,

    /// <summary>
    ///     A duplicate material ID was found.
    /// </summary>
    DuplicateMaterialId,

    /// <summary>
    ///     The price value is invalid.
    /// </summary>
    InvalidPrice,

    /// <summary>
    ///     A required field is empty.
    /// </summary>
    EmptyRequiredField,

    /// <summary>
    ///     The price is out of the valid range.
    /// </summary>
    PriceOutOfRange
}

/// <summary>
///     Represents a validation error found during CSV validation.
/// </summary>
public sealed class CsvValidationError
{
    /// <summary>
    ///     Gets or sets the row number where the error was found.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    ///     Gets or sets the column name where the error was found.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the invalid value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    ///     Gets or sets the type of validation error.
    /// </summary>
    public CsvValidationErrorType ErrorType { get; set; }

    /// <summary>
    ///     Gets or sets a human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
///     Represents a validation warning found during CSV validation.
/// </summary>
public sealed class CsvValidationWarning
{
    /// <summary>
    ///     Gets or sets the row number where the warning was found.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    ///     Gets or sets the column name where the warning was found.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the value that caused the warning.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    ///     Gets or sets a human-readable warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
///     Represents a summary of validation results.
/// </summary>
public sealed class CsvValidationSummary
{
    /// <summary>
    ///     Gets the total number of rows validated.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    ///     Gets the number of valid rows.
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    ///     Gets the number of errors found.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    ///     Gets the number of warnings found.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the CSV is valid for processing.
    /// </summary>
    public bool IsValid => ErrorCount == 0;
}

/// <summary>
///     Represents the result of validating a CSV file.
/// </summary>
public sealed class CsvValidationResult
{
    /// <summary>
    ///     Gets a value indicating whether the CSV is valid for processing.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    ///     Gets the total number of rows in the CSV.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    ///     Gets the number of valid rows.
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    ///     Gets the collection of validation errors.
    /// </summary>
    public List<CsvValidationError> Errors { get; set; } = new();

    /// <summary>
    ///     Gets the collection of validation warnings.
    /// </summary>
    public List<CsvValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    ///     Gets the validation summary.
    /// </summary>
    public CsvValidationSummary Summary { get; set; } = new();

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    /// <returns>A successful CSV validation result.</returns>
    public static CsvValidationResult Success()
    {
        return new CsvValidationResult
        {
            IsValid = true,
            Summary = new CsvValidationSummary { TotalRows = 0, ValidRows = 0, ErrorCount = 0, WarningCount = 0 }
        };
    }
}
