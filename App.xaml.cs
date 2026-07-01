using System;
using System.Windows;
using System.Threading.Tasks;
using Budget.Models;
using Budget.Services;
using Microsoft.Win32;

namespace Budget;

public partial class App : Application
{
	private readonly ThemeSettingsStore _themeSettingsStore = new();

	protected override void OnStartup(StartupEventArgs e)
	{
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

		try
		{
			var themeMode = _themeSettingsStore.Load().ThemeMode;
			ThemeManager.ApplyTheme(themeMode);
			base.OnStartup(e);
			var window = new MainWindow();
			MainWindow = window;
			window.Show();
		}
		catch (Exception ex)
		{
			ShowFatalError(ex);
			Shutdown(-1);
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
		DispatcherUnhandledException -= OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
		base.OnExit(e);
	}

	private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		ShowFatalError(e.Exception);
		e.Handled = true;
		Shutdown(-1);
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
		{
			ShowFatalError(exception);
		}
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		ShowFatalError(e.Exception);
		e.SetObserved();
	}

	private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
	{
		if (ThemeManager.CurrentThemeMode != ThemeMode.System)
		{
			return;
		}

		Dispatcher.Invoke(() => ThemeManager.ApplyTheme(ThemeMode.System));
	}

	private static void ShowFatalError(Exception exception)
	{
		MessageBox.Show(
			$"Budget couldn't start.\n\n{exception.Message}",
			"Budget",
			MessageBoxButton.OK,
			MessageBoxImage.Error);
	}
}

