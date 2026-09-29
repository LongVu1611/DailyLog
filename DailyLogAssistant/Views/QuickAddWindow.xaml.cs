using System.Windows;
using DailyLogAssistant.ViewModels;

namespace DailyLogAssistant.Views;

public partial class QuickAddWindow : Window
{
    private readonly MainViewModel _viewModel;

    public QuickAddWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.QuickAddSaved += OnQuickAddSaved;
        Closed += OnClosed;
    }

    private void OnQuickAddSaved(object? sender, EventArgs e) => DialogResult = true;

    private void OnClosed(object? sender, EventArgs e) =>
        _viewModel.QuickAddSaved -= OnQuickAddSaved;
}
