using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        SourceInitialized += OnSourceInitialized;
        ThemeManager.ThemeApplied += OnThemeApplied;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestoreWindowPlacement();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        ThemeManager.ThemeApplied -= OnThemeApplied;
        SaveWindowPlacement();
        _viewModel.SaveState();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyTitleBarTheme();
    }

    /// <summary>
    /// Scrolls the tab page for every wheel event, before inner controls
    /// (list views, inline-edit text boxes) can swallow it.
    /// </summary>
    private void OnTabScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        // 16 device-independent pixels per line matches ScrollViewer's default;
        // WheelScrollLines is negative when Windows is set to scroll a page at a time.
        var lines = SystemParameters.WheelScrollLines;
        var step = lines < 0
            ? scrollViewer.ViewportHeight
            : lines * 16.0;

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 120.0 * step));
        e.Handled = true;
    }

    private void OnThemeApplied(object? sender, EventArgs e)
    {
        ApplyTitleBarTheme();
    }

    private void ApplyTitleBarTheme()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var useDark = ThemeManager.ResolveEffectiveTheme(ThemeManager.CurrentThemeMode) == ThemeMode.Dark ? 1 : 0;
        _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
    }

    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

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

