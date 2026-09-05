using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.Logging;
using System;
using System.Threading;

namespace ECommons.Throttlers;

/// <summary>
/// <see cref="FrameThrottler{T}"/> 專用的幀時鐘。
/// </summary>
/// <remarks>
/// 🔴 <b>為什麼不用 <c>Svc.PluginInterface.UiBuilder.FrameCount</c></b>：
/// Dalamud 的 <c>UiBuilder.OnDraw()</c> 在三種情況下會提早 <c>return</c> ——
/// ①使用者按熱鍵隱藏 UI ②過場動畫播放中 ③GPose —— 而 <c>FrameCount++</c> 排在那些
/// <c>return</c> 之後，且三個對應設定（<c>ToggleUiHide</c> / <c>ToggleUiHideDuringCutscenes</c> /
/// <c>ToggleUiHideDuringGpose</c>）的預設值都是開啟。
/// <para>
/// ⇒ 過場或隱藏 UI 期間該計數器完全不前進，掛在它上面的節流會<b>永不到期</b>。
/// 艦隊裡把 <see cref="FrameThrottler{T}"/> 包成狀態機總閘門的外掛（<c>Utils.GenericThrottle</c>、
/// <c>DCChange.DCThrottle</c>、<c>Throttles.GenericThrottle</c> 之類）會因此整條流程靜默停住，
/// 而且因為是短路運算的最前段，連後面的守衛都不會被求值。
/// </para>
/// <para>
/// 本類別改為自己在 <see cref="IFramework.Update"/> 裡遞增計數器。該事件在遊戲的 update
/// 迴圈內觸發，與有沒有繪製 UI 無關，在標題／選角畫面同樣照常觸發。
/// </para>
/// <para>
/// ⚠️ <b>遞增函式體內不可有任何條件判斷</b> —— 任何 early return 都會讓時鐘在某些狀態下停住，
/// 而那正是本類別要修掉的失敗形狀。
/// </para>
/// <para>
/// ⚠️ 本時鐘的<b>絕對值</b>與 <c>UiBuilder.FrameCount</c> 無關（自訂閱起算）。
/// 絕不可以「存入時用一個時鐘、讀取時用另一個」—— 兩者數量級不同，會讓節流不是永不到期就是完全失效。
/// </para>
/// </remarks>
internal static class FrameThrottlerClock
{
    /// <summary>自訂閱起算的幀數。跨執行緒讀寫一律走 <see cref="Interlocked"/> / <see cref="Volatile"/>。</summary>
    private static long FrameTicks;

    /// <summary>0 ＝ 未訂閱，1 ＝ 已訂閱。</summary>
    /// <remarks>
    /// 🔴 用 <c>Interlocked.CompareExchange</c> 而不是 <c>bool</c>：
    /// 重複訂閱不是「沒效果」，而是每個 tick 前進 2 ＝ 所有節流時間對半砍。
    /// </remarks>
    private static int Subscribed;

    /// <summary>訂閱成功當下的 <see cref="Environment.TickCount64"/>，供「時鐘從未前進」的診斷用。</summary>
    private static long SubscribedAtMs;

    /// <summary>「時鐘從未前進」的診斷是否已經寫過（只寫一次，不洗 log）。</summary>
    private static int StuckWarningWritten;

    /// <summary>目前的幀數。第一次取用時才訂閱 <see cref="IFramework.Update"/>。</summary>
    internal static long CurrentFrame
    {
        get
        {
            EnsureSubscribed();
            var ticks = Volatile.Read(ref FrameTicks);
            if(ticks == 0) WarnIfClockNeverAdvanced();
            return ticks;
        }
    }

    /// <summary>惰性訂閱，且只會成功訂閱一次。</summary>
    internal static void EnsureSubscribed()
    {
        if(Volatile.Read(ref Subscribed) != 0) return;
        if(Interlocked.CompareExchange(ref Subscribed, 1, 0) != 0) return;
        try
        {
            Svc.Framework.Update += OnFrameworkUpdate;
            Volatile.Write(ref SubscribedAtMs, Environment.TickCount64);
        }
        catch(Exception e)
        {
            // 🔴 訂閱失敗時必須把旗標放回去，讓下一次取用還能再試。
            //    否則旗標停在「已訂閱」而事件其實沒接上 ＝ 時鐘永不前進，
            //    所有以幀為單位的節流都永不到期，而且全程零訊息。
            Volatile.Write(ref Subscribed, 0);
            PluginLog.Error($"[ECommons] FrameThrottler 幀時鐘訂閱 Framework.Update 失敗：{e.Message}");
        }
    }

    /// <summary>🔴 函式體內不可加入任何條件判斷。</summary>
    private static void OnFrameworkUpdate(IFramework framework) => Interlocked.Increment(ref FrameTicks);

    /// <summary>
    /// 讓「訂閱沒生效」這個假設在不成立時看得見。
    /// </summary>
    /// <remarks>
    /// 失敗形式是「所有幀節流永不到期 ⇒ 依賴它的自動化流程靜默停住」，沒有任何例外或錯誤訊息，
    /// 所以這裡主動寫一則 <c>Information</c>（使用者的 LogLevel 收得到，
    /// 而且不像 <c>Debug</c> 那樣會被數十萬行淹沒；也刻意不用 <c>DuoLog</c>，那會洗聊天視窗）。
    /// </remarks>
    private static void WarnIfClockNeverAdvanced()
    {
        if(Volatile.Read(ref StuckWarningWritten) != 0) return;
        var since = Volatile.Read(ref SubscribedAtMs);
        if(since == 0 || Environment.TickCount64 - since < 15000) return;
        if(Interlocked.CompareExchange(ref StuckWarningWritten, 1, 0) != 0) return;
        PluginLog.Information("[ECommons] FrameThrottler 幀時鐘異常：已訂閱 Framework.Update 超過 15 秒，但計數器仍停在 0。所有以幀為單位的節流都會永不到期，依賴它的自動化流程會靜默停住。請把這一行回報給開發者。");
    }

    /// <summary>由 <c>ECommonsMain.Dispose</c> 呼叫。</summary>
    /// <remarks>⚠️ 刻意不歸零 <see cref="FrameTicks"/>：時鐘必須是單調遞增的，否則既有的節流期限會變成未來很遠的值。</remarks>
    internal static void Dispose()
    {
        if(Interlocked.Exchange(ref Subscribed, 0) == 0) return;
        Svc.Framework.Update -= OnFrameworkUpdate;
        Volatile.Write(ref SubscribedAtMs, 0);
        Volatile.Write(ref StuckWarningWritten, 0);
    }
}
