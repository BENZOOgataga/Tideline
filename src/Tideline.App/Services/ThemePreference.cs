using System;
using System.IO;
using Microsoft.UI.Xaml;
using Tideline.Core.Data;

namespace Tideline.App.Services;

/// <summary>
/// Tiny on-disk theme preference. The value is rendered to the WinUI
/// FrameworkElement.RequestedTheme on the active window. The default of
/// Default means "follow Windows", consistent with SPEC section 4.
/// </summary>
public static class ThemePreference
{
    public static string FilePath => Path.Combine(AppPaths.Root, "theme.txt");

    public static ElementTheme Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return ElementTheme.Default;
            string raw = File.ReadAllText(FilePath).Trim();
            return raw switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
        catch
        {
            return ElementTheme.Default;
        }
    }

    public static void Save(ElementTheme theme)
    {
        try
        {
            AppPaths.EnsureDirectories();
            string raw = theme switch
            {
                ElementTheme.Light => "Light",
                ElementTheme.Dark => "Dark",
                _ => "System",
            };
            File.WriteAllText(FilePath, raw);
        }
        catch
        {
            // best effort
        }
    }

    public static void Apply(ElementTheme theme, Window? window)
    {
        if (window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }
}
