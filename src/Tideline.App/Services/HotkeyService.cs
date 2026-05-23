using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Tideline.App.Interop;

namespace Tideline.App.Services;

/// <summary>
/// Registers a single global hotkey and raises <see cref="HotkeyPressed"/> on the UI dispatcher
/// when it fires. Runs its own message loop on a dedicated background thread because
/// RegisterHotKey requires a thread-owned window.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    public const int HotkeyId = 0xC0FE;
    public const string WindowClassName = "TidelineHotkeySink";

    public event Action? HotkeyPressed;
    public bool IsRegistered { get; private set; }
    public string? LastError { get; private set; }

    private readonly DispatcherQueue _uiDispatcher;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private IntPtr _hwnd;
    private Win32.WndProcDelegate? _wndProc; // keep delegate alive
    private bool _disposed;

    public HotkeyService(DispatcherQueue uiDispatcher)
    {
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "TidelineHotkey",
        };
        _thread.SetApartmentState(ApartmentState.STA);
    }

    /// <summary>
    /// Registers Ctrl+Alt+N. Returns true on success. If the hotkey is already taken by
    /// another app, sets <see cref="LastError"/> and returns false. Surface this in
    /// Settings rather than failing silently (see SPEC section 17).
    /// </summary>
    public bool TryRegisterDefault()
    {
        _thread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(3)))
        {
            LastError = "Hotkey thread did not start in time.";
            return false;
        }
        bool ok = Win32.RegisterHotKey(_hwnd, HotkeyId, Win32.MOD_CONTROL | Win32.MOD_ALT | Win32.MOD_NOREPEAT, (uint)Win32.VK_N);
        if (!ok)
        {
            int err = Marshal.GetLastWin32Error();
            LastError = $"RegisterHotKey failed, Win32 error {err}. The Ctrl+Alt+N combination may already be taken.";
            return false;
        }
        IsRegistered = true;
        return true;
    }

    private void MessageLoop()
    {
        _wndProc = WindowProc;
        Win32.WNDCLASS cls = new()
        {
            lpfnWndProc = _wndProc,
            hInstance = Win32.GetModuleHandleW(null),
            lpszClassName = WindowClassName,
        };
        ushort atom = Win32.RegisterClassW(ref cls);
        if (atom == 0)
        {
            LastError = "RegisterClassW failed.";
            _ready.Set();
            return;
        }
        _hwnd = Win32.CreateWindowExW(0, WindowClassName, null, 0, 0, 0, 0, 0,
            Win32.HWND_MESSAGE, IntPtr.Zero, cls.hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
        {
            LastError = "CreateWindowExW failed.";
            _ready.Set();
            return;
        }
        _ready.Set();

        while (Win32.GetMessageW(out Win32.MSG msg, IntPtr.Zero, 0, 0))
        {
            Win32.TranslateMessage(ref msg);
            Win32.DispatchMessageW(ref msg);
        }
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == Win32.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Action? handler = HotkeyPressed;
            if (handler is not null)
            {
                _uiDispatcher.TryEnqueue(() => handler());
            }
            return IntPtr.Zero;
        }
        if (msg == Win32.WM_DESTROY)
        {
            Win32.PostQuitMessage(0);
            return IntPtr.Zero;
        }
        return Win32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hwnd != IntPtr.Zero)
        {
            try
            {
                if (IsRegistered)
                {
                    Win32.UnregisterHotKey(_hwnd, HotkeyId);
                }
                Win32.DestroyWindow(_hwnd);
            }
            catch
            {
                // best effort during shutdown
            }
        }
    }
}
