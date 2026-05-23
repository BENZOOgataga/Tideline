using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tideline.App.Services;

/// <summary>
/// Wraps schtasks.exe to create a per-user logon trigger with a small delay,
/// as required by SPEC section 14. The HKCU Run key is deliberately avoided
/// because it fires too early and competes with other startup programs.
/// </summary>
public sealed class AutoStartService
{
    public const string TaskName = "Tideline-AutoStart";
    public const int DefaultDelaySeconds = 8;

    public bool IsEnabled()
    {
        ProcessStartInfo psi = new("schtasks.exe", $"/Query /TN \"{TaskName}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try
        {
            using Process p = Process.Start(psi)!;
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public (bool Ok, string? Error) Enable(string exePath, int delaySeconds = DefaultDelaySeconds)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return (false, "Tideline executable path not found.");
        }

        // Delete any prior version so re-enabling picks up the current exe path.
        TryDelete();

        string xml = BuildTaskXml(exePath, delaySeconds);
        string xmlPath = Path.Combine(Path.GetTempPath(), $"tideline-autostart-{Guid.NewGuid():N}.xml");
        // Encoding declared in BuildTaskXml must match what we write here,
        // otherwise schtasks.exe can reject the task file on some systems.
        File.WriteAllText(xmlPath, xml, new UTF8Encoding(true));

        try
        {
            ProcessStartInfo psi = new("schtasks.exe", $"/Create /TN \"{TaskName}\" /XML \"{xmlPath}\" /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process p = Process.Start(psi)!;
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(10000);
            if (p.ExitCode == 0) return (true, null);
            return (false, string.IsNullOrWhiteSpace(stderr) ? $"schtasks exited {p.ExitCode}" : stderr.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            try { File.Delete(xmlPath); } catch { }
        }
    }

    public bool Disable() => TryDelete();

    private bool TryDelete()
    {
        ProcessStartInfo psi = new("schtasks.exe", $"/Delete /TN \"{TaskName}\" /F")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try
        {
            using Process p = Process.Start(psi)!;
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildTaskXml(string exePath, int delaySeconds)
    {
        string user = Environment.UserDomainName + "\\" + Environment.UserName;
        string delayIso = delaySeconds > 0 ? $"PT{delaySeconds}S" : "PT0S";
        string nowIso = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Task version=""1.4"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Date>{nowIso}</Date>
    <Author>Tideline</Author>
    <Description>Launch Tideline on user logon with a short delay so it does not fight other startup programs for boot resources.</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <UserId>{System.Security.Principal.WindowsIdentity.GetCurrent().User}</UserId>
      <Delay>{delayIso}</Delay>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>{System.Security.Principal.WindowsIdentity.GetCurrent().User}</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>LeastPrivilege</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{System.Security.SecurityElement.Escape(exePath)}</Command>
    </Exec>
  </Actions>
</Task>";
        return xml;
    }

    public string ExePath
    {
        get
        {
            string? path = Environment.ProcessPath;
            return string.IsNullOrEmpty(path) ? string.Empty : path;
        }
    }
}
