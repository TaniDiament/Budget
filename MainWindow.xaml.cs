using System;
using System.Windows;
using Budget.Models;
using Budget.Services;
using Budget.ViewModels;
using Microsoft.Win32;

namespace Budget;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly BudgetFileExchangeService _fileExchangeService = new();
    private readonly WindowPlacementStore _windowPlacementStore = new();

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestoreWindowPlacement();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowPlacement();
        _viewModel.SaveState();
    }

    private void ImportBudgetClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import budget",
            Filter = "Budget files (*.json;*.csv)|*.json;*.csv|JSON files (*.json)|*.json|CSV files (*.csv)|*.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var state = _fileExchangeService.Import(dialog.FileName);
            _viewModel.ApplyState(state, $"Imported budget from {System.IO.Path.GetFileName(dialog.FileName)}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not import the selected file.\n\n{ex.Message}", "Budget", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportBudgetClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export budget",
            FileName = "budget.json",
            Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _fileExchangeService.Export(_viewModel.SnapshotState(), dialog.FileName);
            _viewModel.SetStatusMessage($"Exported budget to {System.IO.Path.GetFileName(dialog.FileName)}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not export the budget.\n\n{ex.Message}", "Budget", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreWindowPlacement()
    {
        if (!_windowPlacementStore.TryLoad(out var placement))
        {
            return;
        }

        var placementBounds = new Rect(placement.Left, placement.Top, placement.Width, placement.Height);
        var virtualScreenBounds = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        if (!IsValidPlacement(placementBounds) || !placementBounds.IntersectsWith(virtualScreenBounds))
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = placement.Width;
        Height = placement.Height;

        if (!double.IsNaN(placement.Left))
        {
            Left = placement.Left;
        }

        if (!double.IsNaN(placement.Top))
        {
            Top = placement.Top;
        }

        if (string.Equals(placement.WindowState, "Maximized", StringComparison.OrdinalIgnoreCase))
        {
            WindowState = System.Windows.WindowState.Maximized;
        }
    }

    private void SaveWindowPlacement()
    {
        var bounds = WindowState == System.Windows.WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
        var placement = new WindowPlacement
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            WindowState = WindowState.ToString()
        };

        _windowPlacementStore.Save(placement);
    }

    private static bool IsValidPlacement(Rect bounds)
    {
        return !double.IsNaN(bounds.Left) &&
               !double.IsNaN(bounds.Top) &&
               !double.IsNaN(bounds.Width) &&
               !double.IsNaN(bounds.Height) &&
               bounds.Width >= 400 &&
               bounds.Height >= 300;
    }
}

