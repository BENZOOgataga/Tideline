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
    public CaptureService Capture { get; private set; } = null!;
    public HotkeyService Hotkey { get; private set; } = null!;
    public IpcListener? Ipc { get; private set; }
    public UpdateService Updates { get; private set; } = null!;

    /// <summary>
    /// True while QuitApp is tearing the process down. MainWindow's AppWindow.Closing
    /// handler checks this and lets the close go through instead of hiding to tray.
    /// </summary>
    public bool IsShuttingDown { get; private set; }

    private MainWindow? _mainWindow;
    private TrayHost? _tray;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandled;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTask;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        UiDispatcher = DispatcherQueue.GetForCurrentThread();

        Host = AppHost.Boot();
        Host.RunStartupPurge();

        Capture = new CaptureService(Host);
        Capture.NoteSaved += _ => UiDispatcher?.TryEnqueue(() => { /* future: refresh views */ });

        Hotkey = new HotkeyService(UiDispatcher);
        Hotkey.HotkeyPressed += () => Capture.Show();
        if (!Hotkey.TryRegisterDefault())
        {
            Host.HotkeyError = Hotkey.LastError;
        }

        _mainWindow = new MainWindow(Host);
        ThemePreference.Apply(ThemePreference.Load(), _mainWindow);
        _tray = new TrayHost(this);

        Ipc = new IpcListener(this, UiDispatcher);
        Ipc.Start();

        // One-time migration from the legacy Tideline-AutoStart schtasks
        // entry to HKCU\Run. No-op for fresh installs.
        try { new AutoStartService().MigrateLegacyTaskIfPresent(); } catch { }

        Updates = new UpdateService(UiDispatcher);

        // Auto-start launches stay in the tray; the user can click the
        // icon when they want the window. Normal launches show the
        // briefing immediately.
        if (!Program.LaunchedAtStartup)
        {
            ShowMainWindow();
        }

        // Fire the once-per-session update check after the window is up.
        // Silently no-ops in dev builds (no Velopack install metadata).
        Updates.CheckOnceFireAndForget();
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

    public Window? GetActiveAppWindow() => _mainWindow;

    public void TriggerCapture()
    {
        UiDispatcher?.TryEnqueue(() => Capture.Show());
    }

    public void QuitApp()
    {
        IsShuttingDown = true;
        // Schedule the actual teardown on a low-priority queue tick so the
        // tray menu finishes dismissing first; calling Environment.Exit from
        // inside the click handler can leave the flyout half-disposed.
        if (UiDispatcher is null || !UiDispatcher.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                ShutdownNow))
        {
            ShutdownNow();
        }
    }

    private void ShutdownNow()
    {
        try { Ipc?.Dispose(); } catch (Exception ex) { CrashLog.Write("ShutdownNow.Ipc", ex); }
        try { Hotkey?.Dispose(); } catch (Exception ex) { CrashLog.Write("ShutdownNow.Hotkey", ex); }
        try { _tray?.Dispose(); } catch (Exception ex) { CrashLog.Write("ShutdownNow.Tray", ex); }
        try { _mainWindow?.Close(); } catch (Exception ex) { CrashLog.Write("ShutdownNow.Window", ex); }
        try { Host?.Dispose(); } catch (Exception ex) { CrashLog.Write("ShutdownNow.Host", ex); }
        Environment.Exit(0);
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        CrashLog.Write("UI", e.Exception);
        System.Diagnostics.Debug.WriteLine($"[Tideline] Unhandled: {e.Exception}");
        e.Handled = true;
    }

    private void OnAppDomainUnhandled(object sender, System.UnhandledExceptionEventArgs e)
    {
        CrashLog.Write("AppDomain", e.ExceptionObject as Exception);
    }

    private void OnUnobservedTask(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        CrashLog.Write("Task", e.Exception);
        e.SetObserved();
    }
}
