using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Tideline.App.Services;
using Tideline.App.Views;
using Tideline.Core.Data;
using Tideline.Core.Time;

namespace Tideline.App;

public partial class App : Application
{
    public static new App? Current => (App?)Application.Current;

    public AppHost Host { get; private set; } = null!;
    public DispatcherQueue UiDispatcher { get; private set; } = null!;

    private MainWindow? _mainWindow;
    private TrayHost? _tray;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        UiDispatcher = DispatcherQueue.GetForCurrentThread();

        Host = AppHost.Boot();
        Host.RunStartupPurge();

        _mainWindow = new MainWindow(Host);
        _tray = new TrayHost(this);

        ShowMainWindow();
    }

    public void OnSecondaryInstanceActivated(AppActivationArguments args)
    {
        UiDispatcher?.TryEnqueue(ShowMainWindow);
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }
        _mainWindow.AppWindow.Show();
        _mainWindow.Activate();
    }

    public void HideMainWindow()
    {
        _mainWindow?.AppWindow?.Hide();
    }

    public void QuitApp()
    {
        _tray?.Dispose();
        _mainWindow?.Close();
        Host?.Dispose();
        Exit();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Last resort: log to debug output. Avoid crashing the tray-resident app on UI errors.
        System.Diagnostics.Debug.WriteLine($"[Tideline] Unhandled: {e.Exception}");
        e.Handled = true;
    }
}
