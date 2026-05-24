using System;
using System.IO;

namespace Tideline.App.Services;

/// <summary>
/// Resolves the display name and bundled icon paths for the current build.
/// Debug builds show "Tideline Development" with the dev brand assets so a
/// dev run is visually distinct from the installed release at a glance.
/// </summary>
public static class Brand
{
#if DEBUG
    public const string DisplayName = "Tideline Development";
    private const string IcoFile = "Tideline.dev.ico";
    private const string PngFile = "Tideline.dev.png";
#else
    public const string DisplayName = "Tideline";
    private const string IcoFile = "Tideline.ico";
    private const string PngFile = "Tideline.png";
#endif

    public static string IconPath => Path.Combine(AppContext.BaseDirectory, "Assets", IcoFile);
    public static string PngPath  => Path.Combine(AppContext.BaseDirectory, "Assets", PngFile);
}
