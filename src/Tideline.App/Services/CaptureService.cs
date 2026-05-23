using System;
using Tideline.App.Views;
using Tideline.Core.Models;

namespace Tideline.App.Services;

/// <summary>
/// Owns the lifetime of the capture overlay. Only one capture window is open
/// at a time; subsequent triggers re-focus the existing one.
/// </summary>
public sealed class CaptureService
{
    private readonly AppHost _host;
    private CaptureWindow? _current;

    public event Action<Note>? NoteSaved;

    public CaptureService(AppHost host)
    {
        _host = host;
    }

    public void Show()
    {
        if (_current is not null)
        {
            try { _current.Activate(); return; } catch { _current = null; }
        }
        _current = new CaptureWindow(_host);
        _current.NoteSaved += OnNoteSaved;
        _current.Closed += (_, _) => _current = null;
        _current.Activate();
    }

    private void OnNoteSaved(Note note)
    {
        NoteSaved?.Invoke(note);
    }
}
