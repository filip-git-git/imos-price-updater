namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents information about a configuration backup.
/// </summary>
public sealed class BackupInfo
{
    /// <summary>
    ///     Gets the unique identifier for this backup.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets the name of the backup file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the full path to the backup file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets when the backup was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     Gets the size of the backup file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    ///     Gets the name of the source configuration file.
    /// </summary>
    public string SourceConfigFileName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether this was an automatic backup.
    /// </summary>
    public bool IsAutoBackup { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the backup is compressed.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    ///     Initializes a new instance of the BackupInfo class.
    /// </summary>
    public BackupInfo()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Gets the file size formatted as a human-readable string.
    /// </summary>
    public string FormattedSize
    {
        get
        {
            return FileSizeBytes switch
            {
                < 1024 => $"{FileSizeBytes} B",
                < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
                _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F1} GB"
            };
        }
    }
}
