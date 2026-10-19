using Serilog.Events;

namespace ECommons.Logging;

/// <summary>
/// 每次呼叫都可能走到的記錄先判這裡，關著時連內插字串都不組。每次即時讀、不可快取（使用者會在遊戲裡切等級）。
/// 不能用 <c>Svc.Log.MinimumLogLevel</c>：ECommons 把插件層設成 Verbose，它恆真。
/// </summary>
internal static class LogGate
{
    /// <summary>根層會寫出 Verbose，或 <see cref="InternalLog"/> 會收 Verbose（兩邊都不要時 <see cref="PluginLog.Verbose"/> 什麼都不留）。</summary>
    internal static bool VerboseEnabled =>
        global::Dalamud.EntryPoint.LogLevelSwitch.MinimumLevel <= LogEventLevel.Verbose
        || InternalLog.IsCaptureEnabled(LogEventLevel.Verbose);
}
