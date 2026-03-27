using System.Collections.ObjectModel;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the backup manager dialog.
/// </summary>
public sealed class BackupManagerViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupManagerViewModel> _logger;
    private BackupInfo? _selectedBackup;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private int _retentionCount;
    private string _backupDirectory = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the BackupManagerViewModel class.
    /// </summary>
    /// <param name="backupService">The backup service.</param>
    /// <param name="logger">The logger instance.</param>
    public BackupManagerViewModel(IBackupService backupService, ILogger<BackupManagerViewModel> logger)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Backups = new ObservableCollection<BackupInfo>();
        _retentionCount = _backupService.GetRetentionCount();
        _backupDirectory = _backupService.GetBackupDirectory();

        CreateBackupCommand = new RelayCommand(async _ => await ExecuteCreateBackupAsync());
        RestoreCommand = new RelayCommand(async _ => await ExecuteRestoreAsync(), CanExecuteRestore);
        DeleteCommand = new RelayCommand(async _ => await ExecuteDeleteAsync(), CanExecuteDelete);
        RefreshCommand = new RelayCommand(async _ => await ExecuteRefreshAsync());
        SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
    }

    /// <summary>
    ///     Gets the collection of backups.
    /// </summary>
    public ObservableCollection<BackupInfo> Backups { get; }

    /// <summary>
    ///     Gets or sets the selected backup.
    /// </summary>
    public BackupInfo? SelectedBackup
    {
        get => _selectedBackup;
        set
        {
            if (SetProperty(ref _selectedBackup, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the loading indicator.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    ///     Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    ///     Gets or sets the retention count.
    /// </summary>
    public int RetentionCount
    {
        get => _retentionCount;
        set => SetProperty(ref _retentionCount, value);
    }

    /// <summary>
    ///     Gets or sets the backup directory.
    /// </summary>
    public string BackupDirectory
    {
        get => _backupDirectory;
        set => SetProperty(ref _backupDirectory, value);
    }

    /// <summary>
    ///     Gets the create backup command.
    /// </summary>
    public ICommand CreateBackupCommand { get; }

    /// <summary>
    ///     Gets the restore command.
    /// </summary>
    public ICommand RestoreCommand { get; }

    /// <summary>
    ///     Gets the delete command.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    ///     Gets the refresh command.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    ///     Gets the save settings command.
    /// </summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>
    ///     Loads all backups asynchronously.
    /// </summary>
    public async Task LoadBackupsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading backups...";

        try
        {
            var backups = await _backupService.GetAllBackupsAsync();
            Backups.Clear();
            foreach (var backup in backups)
            {
                Backups.Add(backup);
            }

            StatusMessage = $"Loaded {Backups.Count} backups";
            _logger.LogInformation("Loaded {Count} backups", Backups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backups");
            StatusMessage = "Failed to load backups";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    ///     Creates a backup of the specified configuration file.
    /// </summary>
    /// <param name="configFilePath">The path to the configuration file.</param>
    public async Task CreateBackupAsync(string configFilePath)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating backup...";

            await _backupService.CreateBackupAsync(configFilePath, isAutoBackup: false);
            await LoadBackupsAsync();

            StatusMessage = "Backup created successfully";
            _logger.LogInformation("Created manual backup for {ConfigFilePath}", configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            StatusMessage = "Failed to create backup";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    ///     Creates an automatic backup before editing.
    /// </summary>
    /// <param name="configFilePath">The path to the configuration file.</param>
    public async Task CreateAutoBackupAsync(string configFilePath)
    {
        try
        {
            await _backupService.CreateBackupAsync(configFilePath, isAutoBackup: true);
            _logger.LogInformation("Created auto backup for {ConfigFilePath}", configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create auto backup");
            // Don't throw - auto-backup failure shouldn't block editing
        }
    }

    private async Task ExecuteCreateBackupAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Configuration files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Select Configuration File to Backup"
        };

        if (dialog.ShowDialog() == true)
        {
            await CreateBackupAsync(dialog.FileName);
        }
    }

    private bool CanExecuteRestore(object? parameter)
    {
        return SelectedBackup != null;
    }

    private async Task ExecuteRestoreAsync()
    {
        if (SelectedBackup == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Restoring backup...";

            await _backupService.RestoreBackupAsync(SelectedBackup.Id);

            StatusMessage = "Backup restored successfully";
            _logger.LogInformation("Restored backup {BackupId}", SelectedBackup.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup");
            StatusMessage = "Failed to restore backup";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanExecuteDelete(object? parameter)
    {
        return SelectedBackup != null;
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedBackup == null) return;

        try
        {
            await _backupService.DeleteBackupAsync(SelectedBackup.Id);
            Backups.Remove(SelectedBackup);
            SelectedBackup = null;
            StatusMessage = "Backup deleted";
            _logger.LogInformation("Deleted backup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup");
            StatusMessage = "Failed to delete backup";
        }
    }

    private async Task ExecuteRefreshAsync()
    {
        await LoadBackupsAsync();
    }

    private void ExecuteSaveSettings(object? parameter)
    {
        try
        {
            _backupService.SetRetentionCount(RetentionCount);
            _backupService.SetBackupDirectory(BackupDirectory);
            StatusMessage = "Settings saved";
            _logger.LogInformation("Saved backup settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save backup settings");
            StatusMessage = "Failed to save settings";
        }
    }
}
