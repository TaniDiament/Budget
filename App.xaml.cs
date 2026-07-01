using System;
using System.Windows;
using System.Threading.Tasks;

namespace Budget;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

		try
		{
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

	private static void ShowFatalError(Exception exception)
	{
		MessageBox.Show(
			$"Budget couldn't start.\n\n{exception.Message}",
			"Budget",
			MessageBoxButton.OK,
			MessageBoxImage.Error);
	}
}

