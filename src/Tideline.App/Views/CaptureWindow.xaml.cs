using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Tideline.App.Services;
using Tideline.Core.Models;
using Windows.Graphics;
using Windows.System;

namespace Tideline.App.Views;

public sealed partial class CaptureWindow : Window
{
    private readonly AppHost _host;
    private bool _saved;

    public event Action<Note>? NoteSaved;

    public CaptureWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();
        Title = "Tideline capture";

        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Tideline.ico"));

        if (AppWindow.Presenter is OverlappedPresenter overlapped)
        {
            overlapped.SetBorderAndTitleBar(false, false);
            overlapped.IsResizable = false;
            overlapped.IsMaximizable = false;
            overlapped.IsMinimizable = false;
            overlapped.IsAlwaysOnTop = true;
        }

        AppWindow.Resize(new SizeInt32(560, 110));
        CenterOnScreen();

        Activated += OnActivated;
        AppWindow.Closing += OnAppWindowClosing;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            // Deactivation closes the overlay so it never lingers in the user's way.
            Close();
            return;
        }
        CaptureBox.Focus(FocusState.Programmatic);
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
