using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Velopack;
using Velopack.Sources;

namespace Tideline.App.Services;

/// <summary>
/// Launch-time update check via Velopack. SPEC section 19.2 calls for a
/// single, debounced check per session that never blocks launch. Downloads
/// happen in the background; the user accepts via the in-app banner to
/// apply on the next restart.
/// </summary>
public sealed class UpdateService
{
    public const string FeedUrl = "https://github.com/BENZOOgataga/Tideline";

    public string? AvailableVersion { get; private set; }
    public bool HasUpdate => AvailableVersion is not null && _pending is not null;
    public string? LastError { get; private set; }
    public bool IsChecking { get; private set; }
    public bool IsDownloading { get; private set; }

    /// <summary>Raised on the UI dispatcher when an update has been downloaded and is ready to apply.</summary>
    public event Action? UpdateReady;

    /// <summary>Raised on the UI dispatcher when a check finishes, regardless of whether an update was found.</summary>
    public event Action? CheckCompleted;

    private readonly UpdateManager? _mgr;
    private readonly DispatcherQueue _uiDispatcher;
    private UpdateInfo? _pending;
    private bool _checkedThisSession;

    public UpdateService(DispatcherQueue uiDispatcher)
    {
        _uiDispatcher = uiDispatcher;
        try
        {
            _mgr = new UpdateManager(new GithubSource(FeedUrl, accessToken: null, prerelease: false));
        }
        catch (Exception ex)
        {
            LastError = "Update manager init failed: " + ex.Message;
            _mgr = null;
        }
    }

    /// <summary>
    /// Returns true only when this process is the Velopack-installed binary.
    /// Dev / bin/Debug runs have no install metadata and would otherwise
    /// spam the GitHub feed on every save.
    /// </summary>
    public bool IsInstalledBuild => _mgr?.IsInstalled == true;

    public void CheckOnceFireAndForget()
    {
        if (_checkedThisSession) return;
        _checkedThisSession = true;
        _ = Task.Run(CheckAsync);
    }

    /// <summary>
    /// Manual check from the tray menu or Settings; bypasses the per-session
    /// guard so a user request always re-asks the feed.
    /// </summary>
    public void RecheckFireAndForget()
    {
        _checkedThisSession = true;
        _ = Task.Run(CheckAsync);
    }

    private async Task CheckAsync()
    {
        if (_mgr is null || !_mgr.IsInstalled) return;
        try
        {
            IsChecking = true;
            UpdateInfo? info = await _mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            IsChecking = false;
            if (info is null)
            {
                return; // already on the latest version
            }
            _pending = info;
            AvailableVersion = info.TargetFullRelease?.Version?.ToString();

            IsDownloading = true;
            await _mgr.DownloadUpdatesAsync(info).ConfigureAwait(false);
            IsDownloading = false;

            _uiDispatcher.TryEnqueue(() => UpdateReady?.Invoke());
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            CrashLog.Write("UpdateService", ex);
        }
        finally
        {
            IsChecking = false;
            IsDownloading = false;
            _uiDispatcher.TryEnqueue(() => CheckCompleted?.Invoke());
        }
    }

    public void ApplyAndRestart()
    {
        if (_mgr is null || _pending is null) return;
        try
        {
            _mgr.ApplyUpdatesAndRestart(_pending);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            CrashLog.Write("UpdateService.Apply", ex);
        }
    }
}
