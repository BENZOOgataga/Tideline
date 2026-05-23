using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Tideline.CaptureClient;

internal static class Program
{
    private const string PipeName = "tideline";

    private static int Main(string[] args)
    {
        string? text = null;
        bool show = false;
        bool count = false;
        bool includeArchived = false;
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a is "--text" or "-t")
            {
                if (i + 1 >= args.Length) return Usage();
                text = args[++i];
            }
            else if (a is "--show")
            {
                show = true;
            }
            else if (a is "--count")
            {
                count = true;
            }
            else if (a is "--include-archived")
            {
                includeArchived = true;
            }
            else if (a is "--help" or "-h" or "/?")
            {
                return Usage();
            }
            else
            {
                Console.Error.WriteLine($"Unknown arg: {a}");
                return Usage();
            }
        }

        string cmd = count ? "count" : (show ? "show" : "capture");
        object payload = count
            ? new { cmd, includeArchived }
            : new { cmd, text };
        string json = JsonSerializer.Serialize(payload);
        try
        {
            using NamedPipeClientStream client = new(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(2000);
            using StreamWriter writer = new(client, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
            using StreamReader reader = new(client, Encoding.UTF8, false, 1024, leaveOpen: true);
            writer.WriteLine(json);
            string? response = reader.ReadLine();
            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine(response);
            }
            return 0;
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine("Could not connect to Tideline. Is the app running?");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"IPC error: {ex.Message}");
            return 1;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: tideline-capture [--text \"note body\"] [--show] [--count [--include-archived]]");
        Console.Error.WriteLine("       --text/-t           capture a note immediately with the given body");
        Console.Error.WriteLine("       --show              bring the main window to the front");
        Console.Error.WriteLine("       --count             print {\"count\":N}");
        Console.Error.WriteLine("       --include-archived  include archived notes in --count");
        Console.Error.WriteLine("       (no args)           open the empty capture overlay");
        return 64;
    }
}
