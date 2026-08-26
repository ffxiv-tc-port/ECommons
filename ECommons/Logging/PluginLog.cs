using ECommons.DalamudServices;
using Serilog.Events;

namespace ECommons.Logging;

public static class PluginLog
{

    public static void Information(string s)
    {
        Svc.Log.Information($"{s}");
        Capture(s, LogEventLevel.Information);
    }
    public static void Error(string s)
    {
        Svc.Log.Error($"{s}");
        Capture(s, LogEventLevel.Error);
    }
    public static void Fatal(string s)
    {
        Svc.Log.Fatal($"{s}");
        Capture(s, LogEventLevel.Fatal);
    }
    public static void Debug(string s)
    {
        Svc.Log.Debug($"{s}");
        Capture(s, LogEventLevel.Debug);
    }
    public static void Verbose(string s)
    {
        Svc.Log.Verbose($"{s}");
        Capture(s, LogEventLevel.Verbose);
    }
    public static void Warning(string s)
    {
        Svc.Log.Warning($"{s}");
        Capture(s, LogEventLevel.Warning);
    }
    public static void LogInformation(string s)
    {
        Information(s);
    }
    public static void LogError(string s)
    {
        Error(s);
    }
    public static void LogFatal(string s)
    {
        Fatal(s);
    }
    public static void LogDebug(string s)
    {
        Debug(s);
    }
    public static void LogVerbose(string s)
    {
        Verbose(s);
    }
    public static void LogWarning(string s)
    {
        Warning(s);
    }
    public static void Log(string s)
    {
        Information(s);
    }

    /// <summary>
    /// Write side gate for the <see cref="InternalLog"/> ring buffer. Messages below
    /// <see cref="InternalLog.CaptureLevel"/> return right here, so they allocate no closure, queue no
    /// framework thread delegate and never touch the buffer. This only decides whether a message is
    /// kept for the in game log viewer - the Dalamud log call in the callers above already happened
    /// and is not affected by this gate.
    /// </summary>
    private static void Capture(string s, LogEventLevel level)
    {
        if(!InternalLog.IsCaptureEnabled(level)) return;
        Enqueue(s, level);
    }

    /// <summary>
    /// The closure lives in a separate method on purpose. C# hoists captured parameters into a display
    /// class at method entry, so an early return sitting next to the lambda would still pay for that
    /// allocation. Keeping the lambda behind a call means the gated path never builds one.
    /// </summary>
    private static void Enqueue(string s, LogEventLevel level)
    {
        Svc.Framework?.RunOnFrameworkThread(delegate
        {
            InternalLog.Messages.PushBack(new(s, level));
        });
    }
}
