using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Tideline.App.Interop;
using Tideline.App.Services;
using Tideline.Core.Models;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace Tideline.App.Views;

public sealed partial class CaptureWindow : Window
{
    private readonly AppHost _host;
    private bool _saved;
    private bool _decorationsApplied;

    public event Action<Note>? NoteSaved;

    public CaptureWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();
        Title = Brand.DisplayName + " capture";

        if (System.IO.File.Exists(Brand.IconPath))
        {
            AppWindow.SetIcon(Brand.IconPath);
        }

        if (AppWindow.Presenter is OverlappedPresenter overlapped)
        {
            overlapped.SetBorderAndTitleBar(false, false);
            overlapped.IsResizable = false;
            overlapped.IsMaximizable = false;
            overlapped.IsMinimizable = false;
            overlapped.IsAlwaysOnTop = true;
        }

        AppWindow.Resize(new SizeInt32(680, 132));
        CenterOnScreen();
        ApplyWindowDecorations();

        Activated += OnActivated;
        AppWindow.Closing += OnAppWindowClosing;
    }

    private void ApplyWindowDecorations()
    {
        if (_decorationsApplied) return;
        try
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            // Rounded corners on Windows 11. No-op on Windows 10.
            int corner = Win32.DWMWCP_ROUND;
            Win32.DwmSetWindowAttribute(hwnd, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            // Strip the bright system border that shows on borderless windows.
            uint borderColor = Win32.DWMWA_COLOR_NONE;
            Win32.DwmSetWindowAttribute(hwnd, Win32.DWMWA_BORDER_COLOR, ref borderColor, sizeof(uint));
            _decorationsApplied = true;
        }
        catch
        {
            // best effort; OS may be old or DWM unavailable
        }
    }

    private void ForceForegroundAndFocus()
    {
        try
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            // Attach our thread to the current foreground thread so Windows
            // grants us SetForegroundWindow even when the user invoked
            // capture from another app via the global hotkey or pipe.
            IntPtr fg = Win32.GetForegroundWindow();
            uint fgThread = Win32.GetWindowThreadProcessId(fg, out _);
            uint myThread = Win32.GetCurrentThreadId();
            bool attached = false;
            if (fgThread != 0 && fgThread != myThread)
            {
                attached = Win32.AttachThreadInput(fgThread, myThread, true);
            }
            Win32.SetForegroundWindow(hwnd);
            if (attached)
            {
                Win32.AttachThreadInput(fgThread, myThread, false);
            }
        }
        catch
        {
            // ignored
        }
        CaptureBox.Focus(FocusState.Keyboard);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            // Deactivation closes the overlay so it never lingers in the user's way.
            Close();
            return;
        }
        ApplyWindowDecorations();
        ForceForegroundAndFocus();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // The capture overlay must really close (it is not the tray-resident main window).
    }

    private void CaptureBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            SaveAndClose();
            return;
        }
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void SaveAndClose()
    {
        string body = CaptureBox.Text?.Trim() ?? string.Empty;
        if (body.Length == 0)
        {
            Close();
            return;
        }
        Note note = _host.Notes.Create(body);
        // Inline #hashtags are parsed and attached on save so capture
        // stays a single keystroke per SPEC section 7.
        var tagNames = Tideline.Core.Parsing.HashtagParser.Extract(body);
        if (tagNames.Count > 0)
        {
            _host.Tags.ReplaceForNote(note.Id, tagNames);
        }
        _saved = true;
        NoteSaved?.Invoke(note);
        Close();
    }

    public bool WasSaved => _saved;

    private void CenterOnScreen()
    {
        DisplayArea area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        int width = AppWindow.Size.Width;
        int height = AppWindow.Size.Height;
        int x = area.WorkArea.X + ((area.WorkArea.Width - width) / 2);
        int y = area.WorkArea.Y + ((area.WorkArea.Height - height) / 3);
        AppWindow.Move(new PointInt32(x, y));
    }
}
