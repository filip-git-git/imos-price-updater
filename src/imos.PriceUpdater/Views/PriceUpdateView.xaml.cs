using System.Windows;
using IMOS.PriceUpdater.ViewModels;

namespace IMOS.PriceUpdater.Views;

/// <summary>
///     Interaction logic for PriceUpdateView.xaml
/// </summary>
public partial class PriceUpdateView : Window
{
    /// <summary>
    ///     Initializes a new instance of the PriceUpdateView class.
    /// </summary>
    public PriceUpdateView()
    {
        InitializeComponent();
        DataContext = new PriceUpdateViewModel();
    }

    /// <summary>
    ///     Initializes a new instance of the PriceUpdateView class with a specific ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to use.</param>
    public PriceUpdateView(PriceUpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

