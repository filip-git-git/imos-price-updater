using System.Globalization;
using System.IO;
using System.Text;
using IMOS.PriceUpdater.Models;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Implementation of the CSV validation service.
/// </summary>
public sealed class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly ICsvParser _csvParser;

    private const decimal MinPrice = 0.01m;
    private const decimal MaxPrice = 999999.99m;

    /// <summary>
    ///     Initializes a new instance of the ValidationService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="csvParser">The CSV parser service.</param>
    public ValidationService(ILogger<ValidationService> logger, ICsvParser csvParser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _csvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));
    }

    /// <inheritdoc />
    public async Task<CsvValidationResult> ValidateCsvAsync(
        string csvFilePath,
        PriceUpdateConfiguration config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvFilePath);
        ArgumentNullException.ThrowIfNull(config);

        var result = new CsvValidationResult();
        var requiredColumns = GetRequiredColumns(config);

        // Validate structure first
        var structureResult = await ValidateCsvStructureAsync(csvFilePath, cancellationToken);
        result.Errors.AddRange(structureResult.Errors);
        result.Warnings.AddRange(structureResult.Warnings);

        if (!structureResult.IsValid)
        {
            result.IsValid = false;
            UpdateSummary(result);
            return result;
        }

        // Validate prices
        var priceResult = await ValidatePricesAsync(csvFilePath, cancellationToken);
        result.Errors.AddRange(priceResult.Errors);
        result.Warnings.AddRange(priceResult.Warnings);

        // Check duplicates
        var duplicateResult = await CheckDuplicatesAsync(csvFilePath, cancellationToken);
        result.Errors.AddRange(duplicateResult.Errors);

        result.TotalRows = priceResult.TotalRows;
        result.ValidRows = priceResult.ValidRows;
        result.IsValid = result.Errors.Count == 0;

        UpdateSummary(result);

        _logger.LogInformation(
            "CSV validation completed: {IsValid} - {ErrorCount} errors, {WarningCount} warnings",
            result.IsValid,
            result.Errors.Count,
            result.Warnings.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<CsvValidationResult> ValidateCsvStructureAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvValidationResult();

        if (!File.Exists(csvFilePath))
        {
            result.Errors.Add(new CsvValidationError
            {
                RowNumber = 0,
                ColumnName = string.Empty,
                ErrorType = CsvValidationErrorType.MissingColumn,
                Message = $"CSV file not found: {csvFilePath}"
            });
            result.IsValid = false;
            UpdateSummary(result);
            return result;
        }

        try
        {
            // Read header to check columns
            var encoding = _csvParser.DetectEncoding(csvFilePath);
            using var reader = new StreamReader(csvFilePath, encoding);
            var headerLine = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Errors.Add(new CsvValidationError
                {
                    RowNumber = 1,
                    ColumnName = string.Empty,
                    ErrorType = CsvValidationErrorType.MissingColumn,
                    Message = "CSV file is empty or has no header"
                });
                result.IsValid = false;
                UpdateSummary(result);
                return result;
            }

            var headers = ParseCsvLine(headerLine);
            var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);

            // Check for material ID column (any of the accepted names)
            var materialIdColumns = new[] { "MaterialNo", "Material ID", "MaterialNumber" };
            var hasMaterialIdColumn = materialIdColumns.Any(col => headerSet.Contains(col));
            
            // Check for price column
            var hasPriceColumn = headerSet.Contains("Price");
            
            // Both columns are required
            if (!hasMaterialIdColumn)
            {
                result.Errors.Add(new CsvValidationError
                {
                    RowNumber = 1,
                    ColumnName = string.Join(", ", materialIdColumns),
                    ErrorType = CsvValidationErrorType.MissingColumn,
                    Message = $"Missing required column. Expected one of: {string.Join(", ", materialIdColumns)}"
                });
            }
            
            if (!hasPriceColumn)
            {
                result.Errors.Add(new CsvValidationError
                {
                    RowNumber = 1,
                    ColumnName = "Price",
                    ErrorType = CsvValidationErrorType.MissingColumn,
                    Message = "Missing required column: Price"
                });
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate CSV structure: {FilePath}", csvFilePath);
            result.Errors.Add(new CsvValidationError
            {
                RowNumber = 0,
                ColumnName = string.Empty,
                ErrorType = CsvValidationErrorType.InvalidDataType,
                Message = $"Failed to read CSV file: {ex.Message}"
            });
            result.IsValid = false;
        }

        UpdateSummary(result);
        return result;
    }

    /// <inheritdoc />
    public async Task<CsvValidationResult> ValidatePricesAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvValidationResult();

        try
        {
            var rowNumber = 1;
            var validRows = 0;

            await foreach (var row in _csvParser.ParseAsync(csvFilePath, cancellationToken))
            {
                rowNumber++;
                var hasError = false;

                // Find price column
                var priceColumn = row.Values.Keys.FirstOrDefault(k =>
                    k.Equals("Price", StringComparison.OrdinalIgnoreCase));

                if (priceColumn == null)
                {
                    result.Errors.Add(new CsvValidationError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Price",
                        ErrorType = CsvValidationErrorType.MissingColumn,
                        Message = "Price column not found"
                    });
                    hasError = true;
                }
                else if (row.Values.TryGetValue(priceColumn, out var priceValue))
                {
                    // Check for empty price
                    if (string.IsNullOrWhiteSpace(priceValue))
                    {
                        result.Errors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = priceColumn,
                            Value = priceValue,
                            ErrorType = CsvValidationErrorType.EmptyRequiredField,
                            Message = "Price value is empty"
                        });
                        hasError = true;
                    }
                    // Check for valid decimal
                    else if (!decimal.TryParse(priceValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        result.Errors.Add(new CsvValidationError
                        {
                            RowNumber = rowNumber,
                            ColumnName = priceColumn,
                            Value = priceValue,
                            ErrorType = CsvValidationErrorType.InvalidPrice,
                            Message = $"Invalid price format: '{priceValue}'"
                        });
                        hasError = true;
                    }
                    else
                    {
                        // Check price range
                        if (price <= 0)
                        {
                            result.Errors.Add(new CsvValidationError
                            {
                                RowNumber = rowNumber,
                                ColumnName = priceColumn,
                                Value = priceValue,
                                ErrorType = CsvValidationErrorType.PriceOutOfRange,
                                Message = $"Price must be greater than zero: {price}"
                            });
                            hasError = true;
                        }
                        else if (price > MaxPrice)
                        {
                            result.Errors.Add(new CsvValidationError
                            {
                                RowNumber = rowNumber,
                                ColumnName = priceColumn,
                                Value = priceValue,
                                ErrorType = CsvValidationErrorType.PriceOutOfRange,
                                Message = $"Price exceeds maximum allowed value ({MaxPrice}): {price}"
                            });
                            hasError = true;
                        }
                        else if (price < MinPrice)
                        {
                            result.Warnings.Add(new CsvValidationWarning
                            {
                                RowNumber = rowNumber,
                                ColumnName = priceColumn,
                                Value = priceValue,
                                Message = $"Price is below minimum recommended value ({MinPrice}): {price}"
                            });
                        }

                        if (!hasError)
                        {
                            validRows++;
                        }
                    }
                }

                result.TotalRows++;
            }

            result.ValidRows = validRows;
            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate prices: {FilePath}", csvFilePath);
            result.Errors.Add(new CsvValidationError
            {
                RowNumber = 0,
                ColumnName = string.Empty,
                ErrorType = CsvValidationErrorType.InvalidDataType,
                Message = $"Failed to validate prices: {ex.Message}"
            });
            result.IsValid = false;
        }

        UpdateSummary(result);
        return result;
    }

    /// <inheritdoc />
    public async Task<CsvValidationResult> CheckDuplicatesAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvValidationResult();

        try
        {
            var materialIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var rowNumber = 0;

            await foreach (var row in _csvParser.ParseAsync(csvFilePath, cancellationToken))
            {
                rowNumber++;

                // Find material ID column
                var materialColumn = row.Values.Keys.FirstOrDefault(k =>
                    k.Equals("MaterialNo", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Material ID", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("MaterialNumber", StringComparison.OrdinalIgnoreCase));

                if (materialColumn == null)
                {
                    continue;
                }

                if (!row.Values.TryGetValue(materialColumn, out var materialId) ||
                    string.IsNullOrWhiteSpace(materialId))
                {
                    continue;
                }

                if (materialIds.TryGetValue(materialId, out var existingRow))
                {
                    result.Errors.Add(new CsvValidationError
                    {
                        RowNumber = rowNumber,
                        ColumnName = materialColumn,
                        Value = materialId,
                        ErrorType = CsvValidationErrorType.DuplicateMaterialId,
                        Message = $"Duplicate material ID found. First occurrence at row {existingRow}"
                    });
                }
                else
                {
                    materialIds[materialId] = rowNumber;
                }
            }

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check duplicates: {FilePath}", csvFilePath);
            result.Errors.Add(new CsvValidationError
            {
                RowNumber = 0,
                ColumnName = string.Empty,
                ErrorType = CsvValidationErrorType.InvalidDataType,
                Message = $"Failed to check duplicates: {ex.Message}"
            });
            result.IsValid = false;
        }

        UpdateSummary(result);
        return result;
    }

    /// <inheritdoc />
    public bool IsValidationRequired(string csvFilePath, DateTime lastValidation)
    {
        if (!File.Exists(csvFilePath))
        {
            return false;
        }

        var lastWriteTime = File.GetLastWriteTimeUtc(csvFilePath);
        return lastWriteTime > lastValidation;
    }

    /// <summary>
    ///     Gets the required columns from the configuration.
    /// </summary>
    private static string[] GetRequiredColumns(PriceUpdateConfiguration config)
    {
        var columns = new List<string>();

        if (config.ColumnMapping != null)
        {
            if (!string.IsNullOrWhiteSpace(config.ColumnMapping.CsvSearchColumn))
            {
                columns.Add(config.ColumnMapping.CsvSearchColumn);
            }
            if (!string.IsNullOrWhiteSpace(config.ColumnMapping.CsvPriceColumn))
            {
                columns.Add(config.ColumnMapping.CsvPriceColumn);
            }
        }

        return columns.ToArray();
    }

    /// <summary>
    ///     Parses a CSV line handling quoted values.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ';' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    /// <summary>
    ///     Updates the validation summary.
    /// </summary>
    private static void UpdateSummary(CsvValidationResult result)
    {
        result.Summary = new CsvValidationSummary
        {
            TotalRows = result.TotalRows,
            ValidRows = result.ValidRows,
            ErrorCount = result.Errors.Count,
            WarningCount = result.Warnings.Count
        };
    }
}
