using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the database connection configuration view.
/// </summary>
public sealed class ConnectionViewModel : ViewModelBase
{
    private readonly ISqlConnectionService _connectionService;
    private string _serverName = string.Empty;
    private string _selectedDatabase = string.Empty;
    private AuthenticationMode _authenticationMode = AuthenticationMode.SqlAuthentication;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private int _connectionTimeout = 10;
    private bool _isTesting;
    private bool _isConnected;
    private string _connectionStatus = "Not connected";
    private string _errorMessage = string.Empty;
    private ConnectionTestResult? _lastTestResult;
    private CancellationTokenSource? _cancellationTokenSource;

    public ConnectionViewModel() : this(new SqlConnectionService())
    {
    }

    public ConnectionViewModel(ISqlConnectionService connectionService)
    {
        _connectionService = connectionService;

        TestConnectionCommand = new RelayCommand(
            ExecuteTestConnection,
            CanExecuteTestConnection);

        CancelCommand = new RelayCommand(
            ExecuteCancel,
            CanExecuteCancel);

        ConnectCommand = new RelayCommand(
            ExecuteConnect,
            CanExecuteConnect);

        ClearErrorCommand = new RelayCommand(_ => ClearError());
    }

    /// <summary>
    ///     Gets or sets the server name.
    /// </summary>
    public string ServerName
    {
        get => _serverName;
        set
        {
            if (SetProperty(ref _serverName, value))
            {
                OnPropertyChanged(nameof(CanTestConnection));
                OnPropertyChanged(nameof(HasValidConfiguration));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the selected database.
    /// </summary>
    public string SelectedDatabase
    {
        get => _selectedDatabase;
        set
        {
            if (SetProperty(ref _selectedDatabase, value))
            {
                OnPropertyChanged(nameof(CanConnect));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the authentication mode.
    /// </summary>
    public AuthenticationMode AuthenticationMode
    {
        get => _authenticationMode;
        set
        {
            if (SetProperty(ref _authenticationMode, value))
            {
                OnPropertyChanged(nameof(IsSqlAuthentication));
                OnPropertyChanged(nameof(IsWindowsAuthentication));
                OnPropertyChanged(nameof(CanTestConnection));
                OnPropertyChanged(nameof(HasValidConfiguration));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the username for SQL Authentication.
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                OnPropertyChanged(nameof(CanTestConnection));
                OnPropertyChanged(nameof(HasValidConfiguration));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the password for SQL Authentication.
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                OnPropertyChanged(nameof(CanTestConnection));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, value);
    }

    /// <summary>
    ///     Gets a value indicating whether SQL Authentication is selected.
    /// </summary>
    public bool IsSqlAuthentication => AuthenticationMode == AuthenticationMode.SqlAuthentication;

    /// <summary>
    ///     Gets a value indicating whether Windows Authentication is selected.
    /// </summary>
    public bool IsWindowsAuthentication => AuthenticationMode == AuthenticationMode.WindowsAuthentication;

    /// <summary>
    ///     Gets or sets whether a connection test is in progress.
    /// </summary>
    /// <remarks>
    ///     Setter is internal for testing purposes.
    /// </remarks>
    public bool IsTesting
    {
        get => _isTesting;
        internal set
        {
            if (SetProperty(ref _isTesting, value))
            {
                OnPropertyChanged(nameof(IsNotTesting));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether a connection test is not in progress.
    /// </summary>
    public bool IsNotTesting => !IsTesting;

    /// <summary>
    ///     Gets or sets whether a successful connection has been established.
    /// </summary>
    /// <remarks>
    ///     Setter is internal for testing purposes.
    /// </remarks>
    public bool IsConnected
    {
        get => _isConnected;
        internal set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ConnectionStatus = value
                    ? $"Connected to {ServerName}/{SelectedDatabase}"
                    : "Not connected";
            }
        }
    }

    /// <summary>
    ///     Gets or sets the connection status message.
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    /// <summary>
    ///     Gets or sets the error message.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether there is an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    ///     Gets or sets the last connection test result.
    /// </summary>
    public ConnectionTestResult? LastTestResult
    {
        get => _lastTestResult;
        private set => SetProperty(ref _lastTestResult, value);
    }

    /// <summary>
    ///     Gets a value indicating whether the test connection command can execute.
    /// </summary>
    public bool CanTestConnection => !IsTesting && HasValidConfiguration && !string.IsNullOrEmpty(ServerName);

    /// <summary>
    ///     Gets a value indicating whether the connect command can execute.
    /// </summary>
    public bool CanConnect => !string.IsNullOrEmpty(ServerName) && !string.IsNullOrEmpty(SelectedDatabase);

    /// <summary>
    ///     Gets a value indicating whether the user has provided valid configuration.
    /// </summary>
    public bool HasValidConfiguration
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ServerName))
            {
                return false;
            }

            if (AuthenticationMode == AuthenticationMode.SqlAuthentication)
            {
                return !string.IsNullOrWhiteSpace(Username);
            }

            return true;
        }
    }

    /// <summary>
    ///     Gets the test connection command.
    /// </summary>
    public ICommand TestConnectionCommand { get; }

    /// <summary>
    ///     Gets the cancel command.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    ///     Gets the connect command.
    /// </summary>
    public ICommand ConnectCommand { get; }

    /// <summary>
    ///     Gets the clear error command.
    /// </summary>
    public ICommand ClearErrorCommand { get; }

    private bool CanExecuteTestConnection(object? parameter)
    {
        return !IsTesting && HasValidConfiguration && !string.IsNullOrEmpty(ServerName);
    }

    private async void ExecuteTestConnection(object? parameter)
    {
        await TestConnectionAsync();
    }

    private bool CanExecuteCancel(object? parameter)
    {
        return IsTesting;
    }

    private void ExecuteCancel(object? parameter)
    {
        _cancellationTokenSource?.Cancel();
    }

    private bool CanExecuteConnect(object? parameter)
    {
        return !IsTesting && IsConnected && !string.IsNullOrEmpty(SelectedDatabase);
    }

    private void ExecuteConnect(object? parameter)
    {
        // Connection already established via TestConnection
        IsConnected = true;
    }

    /// <summary>
    ///     Tests the database connection asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TestConnectionAsync()
    {
        if (IsTesting)
        {
            return;
        }

        ClearError();
        IsTesting = true;
        IsConnected = false;
        ConnectionStatus = "Testing connection...";

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            var connectionInfo = BuildConnectionInfo();
            var result = await _connectionService.TestConnectionAsync(connectionInfo, cancellationToken);

            LastTestResult = result;

            if (result.IsSuccess)
            {
                IsConnected = true;
                ConnectionStatus = $"Connected to {result.ServerName}/{result.DatabaseName}";
            }
            else
            {
                IsConnected = false;
                ConnectionStatus = "Connection failed";
                ErrorMessage = result.ErrorMessage ?? "Unknown error";
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionStatus = "Connection cancelled";
            ErrorMessage = "The connection attempt was cancelled.";
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Connection failed";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsTesting = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    ///     Builds a connection info object from the current property values.
    /// </summary>
    /// <returns>A SqlConnectionInfo object.</returns>
    public SqlConnectionInfo BuildConnectionInfo()
    {
        return new SqlConnectionInfo
        {
            Server = ServerName,
            Database = SelectedDatabase,
            AuthenticationMode = AuthenticationMode,
            Username = Username,
            Password = Password,
            ConnectionTimeout = ConnectionTimeout
        };
    }

    /// <summary>
    ///     Clears the error message.
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = string.Empty;
        LastTestResult = null;
    }
}

