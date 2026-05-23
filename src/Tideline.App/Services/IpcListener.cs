using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Tideline.App.Services;

/// <summary>
/// Listens on the Windows named pipe described in SPEC section 16. Accepts
/// line-delimited JSON messages of the shape <c>{"cmd":"capture"}</c> or
/// <c>{"cmd":"capture","text":"..."}</c>. The resident app processes the
/// command on the UI dispatcher so capture flows reuse the existing path.
/// </summary>
public sealed class IpcListener : IDisposable
{
    public const string PipeName = "tideline";

    private readonly App _app;
    private readonly DispatcherQueue _uiDispatcher;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private bool _disposed;

    public IpcListener(App app, DispatcherQueue uiDispatcher)
    {
        _app = app;
        _uiDispatcher = uiDispatcher;
    }

    public void Start()
    {
        _loop = Task.Run(() => AcceptLoop(_cts.Token));
    }

    private static PipeOptions PipeFlags()
    {
        // CurrentUserOnly restricts the named pipe ACL to the launching user so
        // other local users / sessions cannot send capture/show/count commands.
        // SPEC section 18.4 expects per-user isolation.
        return PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly;
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeFlags(),
                    inBufferSize: 4096,
                    outBufferSize: 4096);
                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                NamedPipeServerStream connected = pipe;
                pipe = null; // ownership transferred to the handler
                _ = Task.Run(() => HandleClient(connected), token);
            }
            catch (OperationCanceledException)
            {
                pipe?.Dispose();
                return;
            }
            catch (Exception ex)
            {
                pipe?.Dispose();
                System.Diagnostics.Debug.WriteLine($"[Tideline IPC] accept error: {ex.Message}");
                try { await Task.Delay(500, token).ConfigureAwait(false); } catch { return; }
            }
        }
    }

    private async Task HandleClient(NamedPipeServerStream pipe)
    {
        try
        {
            using StreamReader reader = new(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
            using StreamWriter writer = new(pipe, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
            string? line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }
            string? response = await HandleCommandAsync(line).ConfigureAwait(false);
            if (response is not null)
            {
                await writer.WriteLineAsync(response).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                // Bounded drain: a stuck client cannot pin a thread-pool
                // slot longer than the timeout.
                try
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
                    NamedPipeServerStream local = pipe;
                    await Task.Run(local.WaitForPipeDrain, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* client never read; close anyway */ }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tideline IPC] client error: {ex.Message}");
        }
        finally
        {
            try { pipe.Disconnect(); } catch { }
            try { pipe.Dispose(); } catch { }
        }
    }

    private Task<string?> HandleCommandAsync(string json)
    {
        Command? cmd = null;
        try
        {
            cmd = JsonSerializer.Deserialize<Command>(json, JsonOptions);
        }
        catch
        {
            // Malformed messages are ignored, not fatal, per SPEC 16.1.
            return Task.FromResult<string?>(null);
        }
        if (cmd is null || string.IsNullOrEmpty(cmd.Cmd)) return Task.FromResult<string?>(null);

        switch (cmd.Cmd.ToLowerInvariant())
        {
            case "capture":
                if (!string.IsNullOrWhiteSpace(cmd.Text))
                {
                    return MarshalToUi(() =>
                    {
                        var note = _app.Host.Notes.Create(cmd.Text!.Trim());
                        var hashes = Tideline.Core.Parsing.HashtagParser.Extract(cmd.Text);
                        if (hashes.Count > 0)
                        {
                            _app.Host.Tags.ReplaceForNote(note.Id, hashes);
                        }
                        return JsonSerializer.Serialize(new { ok = true, id = note.Id });
                    });
                }
                return MarshalToUi(() => { _app.TriggerCapture(); return JsonSerializer.Serialize(new { ok = true }); });
            case "show":
                return MarshalToUi(() => { _app.ShowMainWindow(); return JsonSerializer.Serialize(new { ok = true }); });
            case "count":
                bool inc = cmd.IncludeArchived;
                return MarshalToUi(() =>
                {
                    int total = _app.Host.Notes.Count(includeArchived: inc);
                    return JsonSerializer.Serialize(new { count = total });
                });
            default:
                return Task.FromResult<string?>(JsonSerializer.Serialize(new { ok = false, error = "unknown command" }));
        }
    }

    private Task<string?> MarshalToUi(Func<string> work)
    {
        TaskCompletionSource<string?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool queued = _uiDispatcher.TryEnqueue(() =>
        {
            try { tcs.TrySetResult(work()); }
            catch (Exception ex) { tcs.TrySetResult(JsonSerializer.Serialize(new { ok = false, error = ex.Message })); }
        });
        if (!queued)
        {
            tcs.TrySetResult(JsonSerializer.Serialize(new { ok = false, error = "ui dispatcher rejected the work" }));
        }
        return tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _cts.Cancel(); } catch { }
        // Wake any pending WaitForConnection by opening a client to ourselves.
        try
        {
            using NamedPipeClientStream client = new(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(50);
        }
        catch { }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class Command
    {
        public string? Cmd { get; set; }
        public string? Text { get; set; }
        public bool IncludeArchived { get; set; }
    }
}
