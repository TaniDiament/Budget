using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Budget.Models;
using Budget.Services;
using Budget.ViewModels;

namespace Budget;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly BudgetFileExchangeService _fileExchangeService = new();
    private readonly WindowPlacementStore _windowPlacementStore = new();

    // Last known normal-state bounds, so a maximized window still saves a sensible
    // restore size (Avalonia has no RestoreBounds equivalent).
    private PixelPoint _normalPosition;
    private Size _normalSize;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _normalSize = new Size(Width, Height);
        RestoreWindowPlacement();

        PositionChanged += OnPositionChanged;
        SizeChanged += OnSizeChanged;
        Closing += OnClosing;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            _normalPosition = e.Point;
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            _normalSize = e.NewSize;
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        SaveWindowPlacement();
        _viewModel.SaveState();
    }

    private void RestoreWindowPlacement()
    {
        if (!_windowPlacementStore.TryLoad(out var placement))
        {
            return;
        }

        if (double.IsNaN(placement.Width) || double.IsNaN(placement.Height) ||
            placement.Width < 400 || placement.Height < 300)
        {
            return;
        }

        Width = placement.Width;
        Height = placement.Height;
        _normalSize = new Size(placement.Width, placement.Height);

        if (!double.IsNaN(placement.Left) && !double.IsNaN(placement.Top))
        {
            try
            {
                var target = new PixelPoint((int)placement.Left, (int)placement.Top);
                if (Screens.All.Any(screen => screen.Bounds.Contains(target)))
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Position = target;
                    _normalPosition = target;
                }
            }
            catch
            {
                // Screen information can be unavailable this early on some platforms;
                // fall back to the default startup location.
            }
        }

        if (string.Equals(placement.WindowState, "Maximized", StringComparison.OrdinalIgnoreCase))
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveWindowPlacement()
    {
        var isNormal = WindowState == WindowState.Normal;
        var position = isNormal ? Position : _normalPosition;
        var size = isNormal ? Bounds.Size : _normalSize;

        _windowPlacementStore.Save(new WindowPlacement
        {
            Left = position.X,
            Top = position.Y,
            Width = size.Width,
            Height = size.Height,
            WindowState = WindowState.ToString()
        });
    }

    private async void ImportBudgetClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import budget",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Budget files") { Patterns = new[] { "*.json", "*.csv" } },
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } }
                }
            });

            var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (path is null)
            {
                return;
            }

            var state = _fileExchangeService.Import(path);
            _viewModel.ApplyState(state, $"Imported budget from {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatusMessage($"Could not import the selected file: {ex.Message}");
        }
    }

    private async void ExportBudgetClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export budget",
                SuggestedFileName = "budget.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } }
                }
            });

            var path = file?.TryGetLocalPath();
            if (path is null)
            {
                return;
            }

            _fileExchangeService.Export(_viewModel.SnapshotState(), path);
            _viewModel.SetStatusMessage($"Exported budget to {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            _viewModel.SetStatusMessage($"Could not export the budget: {ex.Message}");
        }
    }
}
