using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Service for managing configuration backups.
/// </summary>
public interface IBackupService
{
    /// <summary>
    ///     Creates a backup of a configuration file.
    /// </summary>
    /// <param name="configFilePath">The path to the configuration file.</param>
    /// <param name="isAutoBackup">Whether this is an automatic backup.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Information about the created backup.</returns>
    Task<BackupInfo> CreateBackupAsync(
        string configFilePath,
        bool isAutoBackup = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the list of backups for a specific configuration file.
    /// </summary>
    /// <param name="configFileName">The configuration file name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of backup information.</returns>
    Task<List<BackupInfo>> GetBackupsAsync(
        string configFileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all backups.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all backup information.</returns>
    Task<List<BackupInfo>> GetAllBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Restores a configuration from a backup.
    /// </summary>
    /// <param name="backupId">The backup ID to restore.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RestoreBackupAsync(Guid backupId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a specific backup.
    /// </summary>
    /// <param name="backupId">The backup ID to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteBackupAsync(Guid backupId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes multiple backups.
    /// </summary>
    /// <param name="backupIds">The list of backup IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteBackupsAsync(List<Guid> backupIds, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Applies the retention policy to a configuration's backups.
    /// </summary>
    /// <param name="configFileName">The configuration file name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ApplyRetentionPolicyAsync(string configFileName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Compresses a backup file if it exceeds the size threshold.
    /// </summary>
    /// <param name="backupId">The backup ID to compress.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task CompressBackupAsync(Guid backupId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the configured backup directory path.
    /// </summary>
    /// <returns>The backup directory path.</returns>
    string GetBackupDirectory();

    /// <summary>
    ///     Sets the backup directory path.
    /// </summary>
    /// <param name="path">The new backup directory path.</param>
    void SetBackupDirectory(string path);

    /// <summary>
    ///     Gets the configured retention count.
    /// </summary>
    /// <returns>The retention count.</returns>
    int GetRetentionCount();

    /// <summary>
    ///     Sets the retention count.
    /// </summary>
    /// <param name="count">The new retention count.</param>
    void SetRetentionCount(int count);
}
