using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Tideline.App.Services;

/// <summary>
/// Auto-start via HKCU\...\Run so Tideline appears in Task Manager's
/// Startup apps list and stays in sync when the user toggles it from
/// there. The exe is launched with --startup so it self-delays before
/// any heavy work, addressing the SPEC section 14 concern about
/// fighting other startup programs for boot resources. The old
/// schtasks-based task is deleted on Enable / Disable so a one-time
/// transition from earlier versions is automatic.
/// </summary>
public sealed class AutoStartService
{
    public const string StartupArg = "--startup";
    public const int DefaultDelaySeconds = 8;

    // Brand-aware Run value name so dev and release installs have separate
    // Task Manager entries and never collide when both are present.
    public static string RunValueName => Brand.DisplayName;

    private const string LegacyTaskName = "Tideline-AutoStart";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedRunPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public string ExePath
    {
        get
        {
            string? path = Environment.ProcessPath;
            return string.IsNullOrEmpty(path) ? string.Empty : path;
        }
    }

    /// <summary>
    /// Reports the combined state: the Run entry must exist AND Task Manager
    /// must not have flagged us as user-disabled (StartupApproved blob, first
    /// byte == 0x02 means "disabled by user").
    /// </summary>
    public bool IsEnabled()
    {
        try
        {
            using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (run is null) return false;
            object? value = run.GetValue(RunValueName);
            if (value is null) return false;

            using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(StartupApprovedRunPath, writable: false);
            if (approved?.GetValue(RunValueName) is byte[] blob && blob.Length > 0)
            {
                // Task Manager writes 0x02 (default-enabled) or 0x06 when enabled
                // and 0x03 when disabled by the user. The low bit being set marks
                // the disabled state.
                bool disabledByUser = (blob[0] & 0x01) != 0;
                return !disabledByUser;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public (bool Ok, string? Error) Enable(string exePath, int delaySeconds = DefaultDelaySeconds)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !System.IO.File.Exists(exePath))
        {
            return (false, "Tideline executable path not found.");
        }
        try
        {
            using RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)!;
            string command = $"\"{exePath}\" {StartupArg}";
            run.SetValue(RunValueName, command, RegistryValueKind.String);

            // Clear any StartupApproved flag so Task Manager shows us as enabled.
            try
            {
                using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(StartupApprovedRunPath, writable: true);
                if (approved is not null)
                {
                    // 12-byte blob: first DWORD = 0x00000002 (enabled, low bit clear),
                    // remaining 8 bytes = last-disable FILETIME (zeroed on enable).
                    byte[] enabled = new byte[12];
                    enabled[0] = 0x02; enabled[1] = 0x00; enabled[2] = 0x00; enabled[3] = 0x00;
                    approved.SetValue(RunValueName, enabled, RegistryValueKind.Binary);
                }
            }
            catch
            {
                // best-effort; missing approval blob means Task Manager will treat as enabled by default
            }

            // Sweep the legacy schtasks task if a prior install created it.
            TryDeleteLegacyTask();
            _ = delaySeconds; // current default reads from StartupArgsParser; reserved for future per-user override
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public bool Disable()
    {
        bool ok = true;
        try
        {
            using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            run?.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
        catch
        {
            ok = false;
        }
        try
        {
            using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(StartupApprovedRunPath, writable: true);
            approved?.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
        catch
        {
            // ignored
        }
        TryDeleteLegacyTask();
        return ok;
    }

    private static void TryDeleteLegacyTask()
    {
        try
        {
            ProcessStartInfo psi = new("schtasks.exe", $"/Delete /TN \"{LegacyTaskName}\" /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process? p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
        catch
        {
            // task may not exist; ignore
        }
    }

    /// <summary>
    /// One-time migration for users who enabled auto-start on a version
    /// that used the schtasks Task Scheduler task. If the legacy task is
    /// present, we create the Run entry pointing at the current exe and
    /// then sweep the task. A marker file in AppPaths.Root prevents
    /// re-running.
    /// </summary>
    public void MigrateLegacyTaskIfPresent()
    {
        string markerPath = System.IO.Path.Combine(Tideline.Core.Data.AppPaths.Root, "autostart-migrated");
        try
        {
            if (System.IO.File.Exists(markerPath)) return;

            ProcessStartInfo psi = new("schtasks.exe", $"/Query /TN \"{LegacyTaskName}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process? p = Process.Start(psi);
            if (p is null) return;
            p.WaitForExit(5000);
            bool legacyPresent = p.ExitCode == 0;

            if (legacyPresent && !string.IsNullOrEmpty(ExePath))
            {
                Enable(ExePath); // creates Run + sweeps legacy task
            }
        }
        catch
        {
            // best effort
        }
        finally
        {
            try
            {
                Tideline.Core.Data.AppPaths.EnsureDirectories();
                System.IO.File.WriteAllText(markerPath, DateTimeOffset.Now.ToString("O"));
            }
            catch
            {
                // ignored
            }
        }
    }
}
