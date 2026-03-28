using System.Runtime.CompilerServices;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IMOS.PriceUpdater.Tests.Services;

public class ValidationServiceTests
{
    private readonly Mock<ILogger<ValidationService>> _mockLogger;
    private readonly Mock<ICsvParser> _mockCsvParser;
    private readonly ValidationService _service;
    private readonly string _testDataDirectory;

    public ValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ValidationService>>();
        _mockCsvParser = new Mock<ICsvParser>();
        _service = new ValidationService(_mockLogger.Object, _mockCsvParser.Object);
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "validation_tests");
        Directory.CreateDirectory(_testDataDirectory);
    }

    #region Test Data Helpers

    private string CreateTestCsvFile(
        string content,
        [CallerMemberName] string testName = "")
    {
        var filePath = Path.Combine(_testDataDirectory, $"{testName}.csv");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private static string CreateValidCsvContent()
    {
        return "MaterialNo;Price\nMAT-001;100.00\nMAT-002;200.50\nMAT-003;300.75";
    }

    private static PriceUpdateConfiguration CreateValidConfiguration()
    {
        return new PriceUpdateConfiguration
        {
            ColumnMapping = new ColumnMapping
            {
                CsvSearchColumn = "MaterialNo",
                CsvPriceColumn = "Price"
            }
        };
    }

    #endregion

    #region ValidateCsvStructureAsync Tests

    [Fact]
    public async Task ValidateCsvStructureAsync_WithValidCsv_ReturnsValid()
    {
        // Arrange
        var filePath = CreateTestCsvFile(CreateValidCsvContent());
        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);

        // Act
        var result = await _service.ValidateCsvStructureAsync(filePath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateCsvStructureAsync_WithMissingColumns_ReturnsInvalid()
    {
        // Arrange
        var filePath = CreateTestCsvFile("WrongColumn;Price\nMAT-001;100");
        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);

        // Act
        var result = await _service.ValidateCsvStructureAsync(filePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateCsvStructureAsync_WithNonExistentFile_ReturnsInvalid()
    {
        // Act
        var result = await _service.ValidateCsvStructureAsync("C:\\nonexistent\\file.csv");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorType == CsvValidationErrorType.MissingColumn);
    }

    [Fact]
    public async Task ValidateCsvStructureAsync_WithEmptyFile_ReturnsInvalid()
    {
        // Arrange
        _mockCsvParser.Setup(p => p.DetectEncoding(It.IsAny<string>())).Returns(System.Text.Encoding.UTF8);
        var filePath = CreateTestCsvFile("");

        // Act
        var result = await _service.ValidateCsvStructureAsync(filePath);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion

    #region ValidatePricesAsync Tests

    [Fact]
    public async Task ValidatePricesAsync_WithValidPrices_ReturnsValid()
    {
        // Arrange
        var filePath = CreateTestCsvFile(CreateValidCsvContent());
        SetupParserForValidCsv(filePath);

        // Act
        var result = await _service.ValidatePricesAsync(filePath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(3, result.ValidRows);
    }

    [Fact]
    public async Task ValidatePricesAsync_WithInvalidPrice_ReturnsError()
    {
        // Arrange - mock returns invalid price
        var filePath = CreateTestCsvFile("MaterialNo;Price\nMAT-001;invalid\nMAT-002;200");
        var rows = new List<CsvRow>
        {
            new CsvRow(2, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "invalid" }),
            new CsvRow(3, new Dictionary<string, string> { ["MaterialNo"] = "MAT-002", ["Price"] = "200" })
        };
        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);
        _mockCsvParser.Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>())).Returns(rows.ToAsyncEnumerable());

        // Act
        var result = await _service.ValidatePricesAsync(filePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorType == CsvValidationErrorType.InvalidPrice);
    }

    [Fact]
    public async Task ValidatePricesAsync_WithEmptyPrice_ReturnsError()
    {
        // Arrange - mock returns empty price
        var filePath = CreateTestCsvFile("MaterialNo;Price\nMAT-001;\nMAT-002;200");
        var rows = new List<CsvRow>
        {
            new CsvRow(2, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "" }),
            new CsvRow(3, new Dictionary<string, string> { ["MaterialNo"] = "MAT-002", ["Price"] = "200" })
        };
        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);
        _mockCsvParser.Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>())).Returns(rows.ToAsyncEnumerable());

        // Act
        var result = await _service.ValidatePricesAsync(filePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorType == CsvValidationErrorType.EmptyRequiredField);
    }

    [Fact]
    public async Task ValidatePricesAsync_WithNegativePrice_ReturnsError()
    {
        // Arrange - mock returns negative price
        var filePath = CreateTestCsvFile("MaterialNo;Price\nMAT-001;-50\nMAT-002;200");
        var rows = new List<CsvRow>
        {
            new CsvRow(2, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "-50" }),
            new CsvRow(3, new Dictionary<string, string> { ["MaterialNo"] = "MAT-002", ["Price"] = "200" })
        };
        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);
        _mockCsvParser.Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>())).Returns(rows.ToAsyncEnumerable());

        // Act
        var result = await _service.ValidatePricesAsync(filePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorType == CsvValidationErrorType.PriceOutOfRange);
    }

    [Fact]
    public async Task ValidatePricesAsync_WithZeroPrice_ReturnsError()
    {
        // Arrange - mock returns zero price
        var filePath = CreateTestCsvFile("MaterialNo;Price\nMAT-001;0\nMAT-002;200");
        var rows = new List<CsvRow>
        {
            new CsvRow(2, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "0" }),
            new CsvRow(3, new Dictionary<string, string> { ["MaterialNo"] = "MAT-002", ["Price"] = "200" })
        };
        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);
        _mockCsvParser.Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>())).Returns(rows.ToAsyncEnumerable());

        // Act
        var result = await _service.ValidatePricesAsync(filePath);

        // Assert
        Assert.False(result.IsValid); // Invalid - zero price is an error
        Assert.NotEmpty(result.Errors);
    }

    private void SetupParserForValidCsv(string filePath)
    {
        var rows = new List<CsvRow>
        {
            new CsvRow(2, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "100.00" }),
            new CsvRow(3, new Dictionary<string, string> { ["MaterialNo"] = "MAT-002", ["Price"] = "200.50" }),
            new CsvRow(4, new Dictionary<string, string> { ["MaterialNo"] = "MAT-003", ["Price"] = "300.75" })
        };

        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);
        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(rows.ToAsyncEnumerable());
    }

    #endregion

    #region CheckDuplicatesAsync Tests

    [Fact]
    public async Task CheckDuplicatesAsync_WithDuplicates_ReturnsErrors()
    {
        // Arrange
        var filePath = CreateTestCsvFile("MaterialNo;Price\nMAT-001;100\nMAT-001;200\nMAT-002;300");

        var rows = new List<CsvRow>
        {
            new CsvRow(2, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "100" }),
            new CsvRow(3, new Dictionary<string, string> { ["MaterialNo"] = "MAT-001", ["Price"] = "200" }),
            new CsvRow(4, new Dictionary<string, string> { ["MaterialNo"] = "MAT-002", ["Price"] = "300" })
        };

        _mockCsvParser.Setup(p => p.DetectEncoding(filePath)).Returns(System.Text.Encoding.UTF8);
        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(rows.ToAsyncEnumerable());

        // Act
        var result = await _service.CheckDuplicatesAsync(filePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorType == CsvValidationErrorType.DuplicateMaterialId);
    }

    [Fact]
    public async Task CheckDuplicatesAsync_WithoutDuplicates_ReturnsValid()
    {
        // Arrange
        var filePath = CreateTestCsvFile(CreateValidCsvContent());
        SetupParserForValidCsv(filePath);

        // Act
        var result = await _service.CheckDuplicatesAsync(filePath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region IsValidationRequired Tests

    [Fact]
    public void IsValidationRequired_WithNewerFile_ReturnsTrue()
    {
        // Arrange
        var filePath = CreateTestCsvFile(CreateValidCsvContent());
        var lastValidation = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = _service.IsValidationRequired(filePath, lastValidation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidationRequired_WithOlderFile_ReturnsFalse()
    {
        // Arrange
        var filePath = CreateTestCsvFile(CreateValidCsvContent());
        var lastValidation = DateTime.UtcNow.AddHours(1);

        // Act
        var result = _service.IsValidationRequired(filePath, lastValidation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidationRequired_WithNonExistentFile_ReturnsFalse()
    {
        // Act
        var result = _service.IsValidationRequired("C:\\nonexistent\\file.csv", DateTime.UtcNow);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ValidateCsvAsync Tests

    [Fact]
    public async Task ValidateCsvAsync_WithValidCsv_ReturnsValidResult()
    {
        // Arrange
        var filePath = CreateTestCsvFile(CreateValidCsvContent());
        var config = CreateValidConfiguration();
        SetupParserForValidCsv(filePath);

        // Act
        var result = await _service.ValidateCsvAsync(filePath, config);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.Summary);
    }

    #endregion
}
