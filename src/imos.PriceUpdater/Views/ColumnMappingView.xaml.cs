using System.Windows;
using IMOS.PriceUpdater.ViewModels;

namespace IMOS.PriceUpdater.Views;

/// <summary>
///     Interaction logic for ColumnMappingView.xaml
/// </summary>
public partial class ColumnMappingView : Window
{
    public ColumnMappingView()
    {
        InitializeComponent();
    }

    public ColumnMappingView(ColumnMappingViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ColumnMappingViewModel viewModel)
        {
            if (viewModel.IsValidMapping)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}

