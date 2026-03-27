using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IMOS.PriceUpdater.Tests.Services;

public class BackupServiceTests : IDisposable
{
    private readonly Mock<ILogger<BackupService>> _mockLogger;
    private readonly Mock<ICsvParser> _mockCsvParser;
    private readonly BackupService _service;
    private readonly string _testBackupDirectory;

    public BackupServiceTests()
    {
        _mockLogger = new Mock<ILogger<BackupService>>();
        _mockCsvParser = new Mock<ICsvParser>();
        _service = new BackupService(_mockLogger.Object, _mockCsvParser.Object);
        
        // Use reflection to get the backup directory
        var type = typeof(BackupService);
        var field = type.GetField("_backupDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _testBackupDirectory = field?.GetValue(_service) as string ?? string.Empty;
    }

    public void Dispose()
    {
        // Clean up test files
        if (Directory.Exists(_testBackupDirectory))
        {
            foreach (var file in Directory.GetFiles(_testBackupDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    #region Test Data Helpers

    private static string CreateTestConfigFile(string fileName = "test_config.json")
    {
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);
        var content = @"{
            ""connection"": {
                ""server"": ""localhost"",
                ""database"": ""TestDB""
            }
        }";
        File.WriteAllText(tempPath, content);
        return tempPath;
    }

    #endregion

    #region CreateBackupAsync Tests

    [Fact]
    public async Task CreateBackupAsync_WithValidFile_CreatesBackup()
    {
        // Arrange
        var configPath = CreateTestConfigFile();

        // Act
        var backupInfo = await _service.CreateBackupAsync(configPath);

        // Assert
        Assert.NotNull(backupInfo);
        Assert.True(backupInfo.FileSizeBytes > 0);
        Assert.Equal("test_config.json", backupInfo.SourceConfigFileName);
        Assert.False(backupInfo.IsAutoBackup);
        Assert.False(backupInfo.IsCompressed);
        
        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public async Task CreateBackupAsync_WithAutoBackup_SetsFlag()
    {
        // Arrange
        var configPath = CreateTestConfigFile();

        // Act
        var backupInfo = await _service.CreateBackupAsync(configPath, isAutoBackup: true);

        // Assert
        Assert.True(backupInfo.IsAutoBackup);
        
        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public async Task CreateBackupAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.CreateBackupAsync("C:\\nonexistent\\file.json"));
    }

    [Fact]
    public async Task CreateBackupAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateBackupAsync(""));
    }

    #endregion

    #region GetAllBackupsAsync Tests

    [Fact]
    public async Task GetAllBackupsAsync_ReturnsAllBackups()
    {
        // Arrange
        var configPath1 = CreateTestConfigFile($"backup_test1_{Guid.NewGuid():N}.json");
        var configPath2 = CreateTestConfigFile($"backup_test2_{Guid.NewGuid():N}.json");
        await _service.CreateBackupAsync(configPath1);
        await _service.CreateBackupAsync(configPath2);

        // Act
        var backups = await _service.GetAllBackupsAsync();

        // Assert
        Assert.True(backups.Count >= 2);
        
        // Cleanup
        File.Delete(configPath1);
        File.Delete(configPath2);
    }

    #endregion

    #region GetBackupsAsync Tests

    [Fact]
    public async Task GetBackupsAsync_FiltersByConfigFileName()
    {
        // Arrange
        var config1 = CreateTestConfigFile("config1.json");
        var config2 = CreateTestConfigFile("config2.json");
        
        await _service.CreateBackupAsync(config1);
        await _service.CreateBackupAsync(config2);

        // Act
        var backups = await _service.GetBackupsAsync("config1.json");

        // Assert
        Assert.All(backups, b => Assert.Equal("config1.json", b.SourceConfigFileName));
        
        // Cleanup
        File.Delete(config1);
        File.Delete(config2);
    }

    #endregion

    #region DeleteBackupAsync Tests

    [Fact]
    public async Task DeleteBackupAsync_WithExistingBackup_DeletesSuccessfully()
    {
        // Arrange
        var configPath = CreateTestConfigFile();
        var backupInfo = await _service.CreateBackupAsync(configPath);
        var backupId = backupInfo.Id;

        // Act
        await _service.DeleteBackupAsync(backupId);

        // Assert
        var backups = await _service.GetAllBackupsAsync();
        Assert.DoesNotContain(backups, b => b.Id == backupId);
        
        // Cleanup
        File.Delete(configPath);
    }

    #endregion

    #region RestoreBackupAsync Tests

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_RestoresSuccessfully()
    {
        // Arrange
        var configPath = CreateTestConfigFile("restore_test.json");
        var backupInfo = await _service.CreateBackupAsync(configPath);
        
        // Ensure target directory exists
        var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "imosPriceUpdater", "configs");
        Directory.CreateDirectory(targetDir);
        
        // Write current config
        var currentConfigPath = Path.Combine(targetDir, "restore_test.json");
        File.WriteAllText(currentConfigPath, "{}");

        // Act & Assert - should not throw
        await _service.RestoreBackupAsync(backupInfo.Id);
        
        // Cleanup
        File.Delete(configPath);
        if (File.Exists(currentConfigPath))
            File.Delete(currentConfigPath);
    }

    #endregion

    #region BackupDirectory Tests

    [Fact]
    public void GetBackupDirectory_ReturnsConfiguredPath()
    {
        // Act
        var directory = _service.GetBackupDirectory();

        // Assert
        Assert.False(string.IsNullOrEmpty(directory));
    }

    [Fact]
    public void SetBackupDirectory_UpdatesPath()
    {
        // Arrange
        var newPath = Path.Combine(Path.GetTempPath(), "new_backup_dir");

        // Act
        _service.SetBackupDirectory(newPath);

        // Assert
        Assert.Equal(newPath, _service.GetBackupDirectory());
        
        // Cleanup
        if (Directory.Exists(newPath))
            Directory.Delete(newPath);
    }

    #endregion

    #region RetentionCount Tests

    [Fact]
    public void GetRetentionCount_ReturnsDefaultValue()
    {
        // Act
        var count = _service.GetRetentionCount();

        // Assert
        Assert.Equal(10, count);
    }

    [Fact]
    public void SetRetentionCount_UpdatesValue()
    {
        // Act
        _service.SetRetentionCount(20);

        // Assert
        Assert.Equal(20, _service.GetRetentionCount());
    }

    [Fact]
    public void SetRetentionCount_WithZero_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.SetRetentionCount(0));
    }

    #endregion
}
