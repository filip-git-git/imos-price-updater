using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     CSV parser implementation using CsvHelper library.
/// </summary>
public sealed class CsvParser : ICsvParser
{
    private const int DefaultBatchSize = 500;
    private readonly char _defaultDelimiter;

    /// <summary>
    ///     Initializes a new instance of the CsvParser class.
    /// </summary>
    /// <param name="delimiter">The delimiter to use for parsing. Defaults to semicolon.</param>
    public CsvParser(char delimiter = ';')
    {
        _defaultDelimiter = delimiter;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CsvRow> ParseAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var encoding = DetectEncoding(filePath);
        var config = CreateCsvConfiguration();

        using var reader = new StreamReader(filePath, encoding);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var lineNumber = 1; // Header is line 1

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
            {
                var value = csv.GetField(header);
                if (!string.IsNullOrEmpty(value))
                {
                    values[header] = value;
                }
            }

            yield return new CsvRow(lineNumber, values);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CsvRow[]> ParseInBatchesAsync(
        string filePath,
        int batchSize = DefaultBatchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var batch = new List<CsvRow>(batchSize);

        await foreach (var row in ParseAsync(filePath, cancellationToken))
        {
            batch.Add(row);

            if (batch.Count >= batchSize)
            {
                yield return batch.ToArray();
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            yield return batch.ToArray();
        }
    }

    /// <inheritdoc />
    public Encoding DetectEncoding(string filePath)
    {
        // Read first 4 bytes to detect BOM
        var bom = new byte[4];
        using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            file.Read(bom, 0, 4);
        }

        // UTF-32 BE
        if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
        {
            return new UTF32Encoding(true, true);
        }

        // UTF-32 LE
        if (bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
        {
            return new UTF32Encoding(false, true);
        }

        // UTF-8 BOM
        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        // UTF-16 BE
        if (bom[0] == 0xFE && bom[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        // UTF-16 LE
        if (bom[0] == 0xFF && bom[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        // Try to detect ANSI by reading the file and checking for invalid UTF-8
        return DetectEncodingWithoutBom(filePath);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateColumnsAsync(
        string filePath,
        string[] requiredColumns,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requiredColumns);

        var encoding = DetectEncoding(filePath);
        var config = CreateCsvConfiguration();

        using var reader = new StreamReader(filePath, encoding);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);

        foreach (var column in requiredColumns)
        {
            if (!headerSet.Contains(column))
            {
                return false;
            }
        }

        return true;
    }

    private static CsvConfiguration CreateCsvConfiguration()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            BadDataFound = context => throw new CsvBadDataException(
                $"Bad data found at line {context.Context?.Parser?.Row}: '{context.RawRecord}'"),
            MissingFieldFound = context => throw new CsvMissingFieldException(
                $"Missing field found at line {context.Context?.Parser?.Row}"),
            HeaderValidated = null
        };

        return config;
    }

    private static Encoding DetectEncodingWithoutBom(string filePath)
    {
        // Read entire file content
        var content = File.ReadAllBytes(filePath);

        // Check if valid UTF-8
        if (IsValidUtf8(content))
        {
            return Encoding.UTF8;
        }

        // Default to ANSI (Windows-1252 for Western European)
        return Encoding.GetEncoding(1252);
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        var i = 0;
        while (i < bytes.Length)
        {
            if (bytes[i] <= 0x7F)
            {
                i++;
            }
            else if (bytes[i] >= 0xC2 && bytes[i] <= 0xDF)
            {
                // 2-byte sequence
                if (i + 1 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF)
                {
                    return false;
                }

                i += 2;
            }
            else if (bytes[i] >= 0xE0 && bytes[i] <= 0xEF)
            {
                // 3-byte sequence
                if (i + 2 >= bytes.Length
                    || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF
                    || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF)
                {
                    return false;
                }

                i += 3;
            }
            else if (bytes[i] >= 0xF0 && bytes[i] <= 0xF4)
            {
                // 4-byte sequence
                if (i + 3 >= bytes.Length
                    || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF
                    || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF
                    || bytes[i + 3] < 0x80 || bytes[i + 3] > 0xBF)
                {
                    return false;
                }

                i += 4;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
///     Exception thrown when bad data is found in the CSV file.
/// </summary>
public sealed class CsvBadDataException : Exception
{
    public CsvBadDataException(string message) : base(message)
    {
    }
}

/// <summary>
///     Exception thrown when a required field is missing from the CSV file.
/// </summary>
public sealed class CsvMissingFieldException : Exception
{
    public CsvMissingFieldException(string message) : base(message)
    {
    }
}

