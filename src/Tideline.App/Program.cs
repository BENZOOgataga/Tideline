using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace Tideline.App;

public static class Program
{
    public const string SingleInstanceKey = "tideline-single-instance";

    [STAThread]
    private static int Main(string[] args)
    {
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
