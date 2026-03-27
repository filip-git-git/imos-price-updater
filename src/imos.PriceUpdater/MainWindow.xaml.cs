using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using IMOS.PriceUpdater.ViewModels;

namespace IMOS.PriceUpdater;

public partial class MainWindow : Window
{
    private readonly SqlConnectionService _connectionService = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        var configPath = GetConfigFilePath();
        if (string.IsNullOrEmpty(configPath))
        {
            ConnectionStatusText.Text = "Select configuration file first";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }

        try
        {
            ConnectionStatusText.Text = "Testing connection...";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<PriceUpdateConfiguration>(json, JsonOptions);

            if (config?.SqlConnection == null)
            {
                ConnectionStatusText.Text = "Invalid configuration file";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            var result = await _connectionService.TestConnectionAsync(config.SqlConnection);

            if (result.IsSuccess)
            {
                ConnectionStatusText.Text = $"Connected to {result.ServerName}/{result.DatabaseName}";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ConnectionStatusText.Text = $"Connection failed: {result.ErrorMessage}";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = $"Error: {ex.Message}";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private string? GetConfigFilePath()
    {
        if (DataContext is MainViewModel vm)
        {
            return vm.ConfigFilePath;
        }
        return null;
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }
}
