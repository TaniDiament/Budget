using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Budget.Models;
using Budget.Services;

namespace Budget;

public partial class App : Application
{
    private readonly ThemeSettingsStore _themeSettingsStore = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ThemeManager.ApplyTheme(_themeSettingsStore.Load().ThemeMode);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        if (PlatformSettings is { } platformSettings)
        {
            platformSettings.ColorValuesChanged += (_, _) =>
            {
                ThemeManager.RaiseSystemThemeChanged();
                if (ThemeManager.CurrentThemeMode == ThemeMode.System)
                {
                    ThemeManager.ApplyTheme(ThemeMode.System);
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
