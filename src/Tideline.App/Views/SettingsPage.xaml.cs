using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Tideline.App.Services;

namespace Tideline.App.Views;

public sealed partial class SettingsPage : Page
{
    private AppHost? _host;
    private readonly AutoStartService _autoStart = new();
    private bool _suppressToggle;
    private bool _suppressTheme;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = e.Parameter as AppHost;

        if (App.Current?.Updates is { } updates)
        {
            updates.CheckCompleted -= OnUpdateCheckCompleted;
            updates.CheckCompleted += OnUpdateCheckCompleted;
        }
        if (_host is not null)
        {
            DbPathText.Text = _host.Database.DatabasePath;
            if (!string.IsNullOrWhiteSpace(_host.HotkeyError))
            {
                HotkeyWarning.Message = _host.HotkeyError;
                HotkeyWarning.IsOpen = true;
            }
        }
        _suppressToggle = true;
        AutoStartToggle.IsOn = _autoStart.IsEnabled();
        _suppressToggle = false;

        _suppressTheme = true;
        ThemeChoice.SelectedIndex = ThemePreference.Load() switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0,
        };
        _suppressTheme = false;

        PopulateAbout();
        base.OnNavigatedTo(e);
    }

    private void PopulateAbout()
    {
        AppNameText.Text = Brand.DisplayName;

        Assembly asm = typeof(SettingsPage).Assembly;
        string informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        string file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty;
        string display = !string.IsNullOrEmpty(informational) ? informational : (file.Length > 0 ? file : "unknown");
        VersionText.Text = display;

#if DEBUG
        BuildText.Text = "Debug";
#else
        BuildText.Text = "Release";
#endif

        UpdateService? updates = App.Current?.Updates;
        if (updates is null)
        {
            InstallText.Text = "Update service not initialised";
            UpdateStatusText.Text = "Unavailable";
        }
        else if (!updates.IsInstalledBuild)
        {
            InstallText.Text = "Source / dev build (no Velopack install)";
            UpdateStatusText.Text = "Skipped (dev build)";
            CheckUpdatesButton.IsEnabled = false;
        }
        else
        {
            InstallText.Text = $"Installed via Velopack at {AppContext.BaseDirectory}";
            UpdateStatusText.Text = updates.HasUpdate
                ? $"Update {updates.AvailableVersion} ready to install"
                : updates.IsChecking ? "Checking..."
                : updates.IsDownloading ? "Downloading..."
                : updates.LastError is { } err ? $"Error: {err}"
                : "Up to date";
        }
    }

    private void AutoStartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle) return;
        AutoStartWarning.IsOpen = false;
        AutoStartWarning.Message = string.Empty;
        if (AutoStartToggle.IsOn)
        {
            var (ok, err) = _autoStart.Enable(_autoStart.ExePath);
            if (!ok)
            {
                _suppressToggle = true;
                AutoStartToggle.IsOn = false;
                _suppressToggle = false;
                AutoStartWarning.Title = "Could not enable auto-start";
                AutoStartWarning.Message = err ?? "schtasks failed.";
                AutoStartWarning.IsOpen = true;
            }
        }
        else
        {
            _autoStart.Disable();
        }
    }

    private void ThemeChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressTheme) return;
        ElementTheme theme = ThemeChoice.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
        ThemePreference.Save(theme);
        ThemePreference.Apply(theme, App.Current?.GetActiveAppWindow());
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = "Checking...";
        // Manual user request: bypass the once-per-session guard.
        // CheckCompleted will fire PopulateAbout when the async work finishes.
        App.Current?.Updates?.RecheckFireAndForget();
    }

    private void OnUpdateCheckCompleted() => PopulateAbout();

    private void OpenReleases_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/BENZOOgataga/Tideline/releases",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            CrashLog.Write("OpenReleases", ex);
        }
    }
}
