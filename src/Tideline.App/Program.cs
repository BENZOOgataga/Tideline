using System;
using System.Linq;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Tideline.App.Services;
using Velopack;

namespace Tideline.App;

public static class Program
{
    public const string SingleInstanceKey = "tideline-single-instance";

    /// <summary>True when the process was launched by the auto-start hook (HKCU\Run + --startup).</summary>
    public static bool LaunchedAtStartup { get; private set; }

    [STAThread]
    private static int Main(string[] args)
    {
        // VelopackApp must run before any UI work so install / update /
        // uninstall hook arguments are handled and the app may exit early
        // during a Velopack lifecycle event.
        VelopackApp.Build().Run();

        LaunchedAtStartup = args.Any(a => string.Equals(a, AutoStartService.StartupArg, StringComparison.OrdinalIgnoreCase));
        if (LaunchedAtStartup)
        {
            // Self-delay so we do not fight other auto-start apps for boot
            // resources (the SPEC section 14 concern that originally pushed
            // us toward Task Scheduler, now mitigated here).
            Thread.Sleep(TimeSpan.FromSeconds(AutoStartService.DefaultDelaySeconds));
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();

        bool isRedirect = DecideRedirection();
        if (isRedirect)
        {
            return 0;
        }

        Application.Start(p =>
        {
            DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    private static bool DecideRedirection()
    {
        AppActivationArguments activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);
        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;
            return false;
        }
        keyInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
        return true;
    }

    private static void OnActivated(object? sender, AppActivationArguments args)
    {
        App.Current?.OnSecondaryInstanceActivated(args);
    }
}
