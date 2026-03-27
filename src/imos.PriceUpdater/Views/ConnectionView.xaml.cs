using System.Windows;
using System.Windows.Controls;
using IMOS.PriceUpdater.ViewModels;

namespace IMOS.PriceUpdater.Views;

/// <summary>
///     Interaction logic for ConnectionView.xaml
/// </summary>
public partial class ConnectionView : Window
{
    public ConnectionView()
    {
        InitializeComponent();
    }

    public ConnectionView(ConnectionViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionViewModel viewModel)
        {
            if (viewModel.IsConnected)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}

