using System.IO;
using System.IO.Compression;
using System.Text.Json;
using IMOS.PriceUpdater.Models;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Implementation of the backup service for managing configuration backups.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly ICsvParser _csvParser;
    private string _backupDirectory;
    private int _retentionCount;
    private const long CompressionThresholdBytes = 1024 * 1024; // 1MB

    /// <summary>
    ///     Initializes a new instance of the BackupService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="csvParser">The CSV parser service.</param>
    public BackupService(ILogger<BackupService> logger, ICsvParser csvParser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _csvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "imosPriceUpdater",
            "backups");
        _retentionCount = 10;

        EnsureDirectoryExists();
    }

    /// <inheritdoc />
    public async Task<BackupInfo> CreateBackupAsync(
        string configFilePath,
        bool isAutoBackup = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configFilePath);

        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException("Configuration file not found.", configFilePath);
        }

        var fileInfo = new FileInfo(configFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var suffix = isAutoBackup ? "_autobackup" : "";
        var extension = fileInfo.Extension;
        var fileName = $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}_{timestamp}{suffix}{extension}";
        var backupPath = Path.Combine(_backupDirectory, fileName);

        // Check if compression is needed
        var shouldCompress = fileInfo.Length >= CompressionThresholdBytes;
        if (shouldCompress)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName) + ".zip";
            backupPath = Path.Combine(_backupDirectory, fileName);
        }

        try
        {
            if (shouldCompress)
            {
                await CompressFileAsync(configFilePath, backupPath, cancellationToken);
            }
            else
            {
                await Task.Run(() => File.Copy(configFilePath, backupPath, overwrite: false), cancellationToken);
            }

            var backupInfo = new BackupInfo
            {
                FileName = fileName,
                FilePath = backupPath,
                CreatedAt = DateTime.UtcNow,
                FileSizeBytes = new FileInfo(backupPath).Length,
                SourceConfigFileName = fileInfo.Name,
                IsAutoBackup = isAutoBackup,
                IsCompressed = shouldCompress
            };

            await SaveBackupInfoAsync(backupInfo, cancellationToken);

            _logger.LogInformation(
                "Created backup: {BackupName} ({Size}) for {SourceFile}",
                backupInfo.FileName,
                backupInfo.FormattedSize,
                backupInfo.SourceConfigFileName);

            return backupInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for: {ConfigFilePath}", configFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<BackupInfo>> GetBackupsAsync(
        string configFileName,
        CancellationToken cancellationToken = default)
    {
        var allBackups = await GetAllBackupsAsync(cancellationToken);
        return allBackups
            .Where(b => b.SourceConfigFileName.Equals(configFileName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<BackupInfo>> GetAllBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backups = new List<BackupInfo>();
        var metadataFile = GetMetadataFilePath();

        if (!File.Exists(metadataFile))
        {
            return backups;
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataFile, cancellationToken);
            var savedBackups = JsonSerializer.Deserialize<List<BackupInfo>>(json);
            if (savedBackups != null)
            {
                // Filter to only existing files
                backups = savedBackups
                    .Where(b => File.Exists(b.FilePath) || File.Exists(b.FilePath.Replace(".zip", ".json")))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load backup metadata");
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    /// <inheritdoc />
    public async Task RestoreBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        var backup = (await GetAllBackupsAsync(cancellationToken))
            .FirstOrDefault(b => b.Id == backupId);

        if (backup == null)
        {
            throw new FileNotFoundException("Backup not found.", backupId.ToString());
        }

        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "imosPriceUpdater",
            "configs",
            backup.SourceConfigFileName);

        // Create backup of current config before restoring
        if (File.Exists(targetPath))
        {
            await CreateBackupAsync(targetPath, isAutoBackup: true, cancellationToken);
        }

        // Ensure target directory exists
        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (backup.IsCompressed)
        {
            await ExtractFileAsync(backup.FilePath, targetPath, cancellationToken);
        }
        else
        {
            await Task.Run(() => File.Copy(backup.FilePath, targetPath, overwrite: true), cancellationToken);
        }

        _logger.LogInformation("Restored backup: {BackupName} to {TargetPath}", backup.FileName, targetPath);
    }

    /// <inheritdoc />
    public async Task DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        await DeleteBackupsAsync(new List<Guid> { backupId }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteBackupsAsync(List<Guid> backupIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backupIds);
        var allBackups = await GetAllBackupsAsync(cancellationToken);

        foreach (var id in backupIds)
        {
            var backup = allBackups.FirstOrDefault(b => b.Id == id);
            if (backup == null)
            {
                _logger.LogWarning("Backup not found for deletion: {BackupId}", id);
                continue;
            }

            try
            {
                if (File.Exists(backup.FilePath))
                {
                    await Task.Run(() => File.Delete(backup.FilePath), cancellationToken);
                }
                if (backup.IsCompressed && File.Exists(backup.FilePath.Replace(".zip", ".json")))
                {
                    await Task.Run(() => File.Delete(backup.FilePath.Replace(".zip", ".json")), cancellationToken);
                }

                _logger.LogInformation("Deleted backup: {BackupName}", backup.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup: {BackupId}", id);
            }
        }

        // Update metadata
        var remainingBackups = allBackups.Where(b => !backupIds.Contains(b.Id)).ToList();
        await SaveAllBackupsAsync(remainingBackups, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ApplyRetentionPolicyAsync(string configFileName, CancellationToken cancellationToken = default)
    {
        var backups = await GetBackupsAsync(configFileName, cancellationToken);
        var autoBackups = backups.Where(b => b.IsAutoBackup).ToList();
        var manualBackups = backups.Where(b => !b.IsAutoBackup).ToList();

        // Keep only the most recent auto backups based on retention count
        var autoToDelete = autoBackups.Skip(_retentionCount).ToList();
        if (autoToDelete.Any())
        {
            await DeleteBackupsAsync(autoToDelete.Select(b => b.Id).ToList(), cancellationToken);
        }

        // For manual backups, keep all
        _logger.LogInformation(
            "Applied retention policy to {ConfigFileName}: deleted {Count} auto backups",
            configFileName,
            autoToDelete.Count);
    }

    /// <inheritdoc />
    public async Task CompressBackupAsync(Guid backupId, CancellationToken cancellationToken = default)
    {
        var allBackups = await GetAllBackupsAsync(cancellationToken);
        var backup = allBackups.FirstOrDefault(b => b.Id == backupId);

        if (backup == null || backup.IsCompressed)
        {
            return;
        }

        var originalPath = backup.FilePath;
        var compressedPath = backup.FilePath.Replace(".json", ".zip").Replace(".config", ".zip");

        try
        {
            await CompressFileAsync(originalPath, compressedPath, cancellationToken);

            backup.FilePath = compressedPath;
            backup.IsCompressed = true;
            backup.FileSizeBytes = new FileInfo(compressedPath).Length;

            await Task.Run(() => File.Delete(originalPath), cancellationToken);
            await SaveAllBackupsAsync(allBackups, cancellationToken);

            _logger.LogInformation("Compressed backup: {BackupName}", backup.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress backup: {BackupId}", backupId);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetBackupDirectory()
    {
        return _backupDirectory;
    }

    /// <inheritdoc />
    public void SetBackupDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _backupDirectory = path;
        EnsureDirectoryExists();
    }

    /// <inheritdoc />
    public int GetRetentionCount()
    {
        return _retentionCount;
    }

    /// <inheritdoc />
    public void SetRetentionCount(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Retention count must be greater than zero.", nameof(count));
        }
        _retentionCount = count;
    }

    /// <summary>
    ///     Gets the path to the backup metadata file.
    /// </summary>
    private string GetMetadataFilePath()
    {
        return Path.Combine(_backupDirectory, "backups_metadata.json");
    }

    /// <summary>
    ///     Ensures the backup directory exists.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
            _logger.LogDebug("Created backup directory: {Directory}", _backupDirectory);
        }
    }

    /// <summary>
    ///     Compresses a file to a ZIP archive.
    /// </summary>
    private static async Task CompressFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var sourceStream = File.OpenRead(sourcePath);
        await using var destinationStream = File.Create(destinationPath);
        await using var gzipStream = new GZipStream(destinationStream, CompressionLevel.Optimal);
        await sourceStream.CopyToAsync(gzipStream, cancellationToken);
    }

    /// <summary>
    ///     Extracts a file from a ZIP archive.
    /// </summary>
    private static async Task ExtractFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var sourceStream = File.OpenRead(sourcePath);
        await using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
        await using var destinationStream = File.Create(destinationPath);
        await gzipStream.CopyToAsync(destinationStream, cancellationToken);
    }

    /// <summary>
    ///     Saves a single backup info to metadata.
    /// </summary>
    private async Task SaveBackupInfoAsync(BackupInfo backupInfo, CancellationToken cancellationToken)
    {
        var allBackups = await GetAllBackupsAsync(cancellationToken);
        allBackups.Add(backupInfo);
        await SaveAllBackupsAsync(allBackups, cancellationToken);
    }

    /// <summary>
    ///     Saves all backup info to metadata file.
    /// </summary>
    private async Task SaveAllBackupsAsync(List<BackupInfo> backups, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(backups, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(GetMetadataFilePath(), json, cancellationToken);
    }
}
