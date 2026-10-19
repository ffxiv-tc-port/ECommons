using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
#nullable disable

namespace ECommons.GameHelpers;

/// <summary>
/// FFXIVClientStructs 靜態位址與成員函式位址的不擲例外存取。
/// </summary>
/// <remarks>
/// 契約:位址沒解出(遊戲改版後特徵碼對不上)時回 null/false —— CS 產生的 <c>Instance()</c> 與成員函式在那種情況是擲例外。
/// 呼叫端必須看回傳值。不快取:每次現讀位址,呼叫端不可跨幀保存拿到的指標。
/// 可從任何執行緒呼叫;每個沒解出的位址只記一次 log。
/// </remarks>
internal static unsafe class CSAddress
{
    private static readonly ConcurrentDictionary<string, byte> Reported = new();

    /// <summary><paramref name="address"/>(靜態位址或成員函式指標)有沒有解出。<paramref name="name"/> 省略時是引數的原始碼文字。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Resolved(void* address, [CallerArgumentExpression(nameof(address))] string name = null)
    {
        if(address != null) return true;
        ReportOnce(name);
        return false;
    }

    // ── 靜態位址就是物件本身:解出來就不是 null ──

    public static bool TryPlayerState(out PlayerState* instance)
        => Resolved(instance = PlayerState.StaticAddressPointers.pInstance, "PlayerState.Instance");

    public static bool TryActionManager(out ActionManager* instance)
        => Resolved(instance = ActionManager.StaticAddressPointers.pInstance, "ActionManager.Instance");

    public static bool TryInventoryManager(out InventoryManager* instance)
        => Resolved(instance = InventoryManager.StaticAddressPointers.pInstance, "InventoryManager.Instance");

    public static bool TryGameMain(out GameMain* instance)
        => Resolved(instance = GameMain.StaticAddressPointers.pInstance, "GameMain.Instance");

    public static bool TryGameObjectManager(out GameObjectManager* instance)
        => Resolved(instance = GameObjectManager.StaticAddressPointers.pInstance, "GameObjectManager.Instance");

    public static bool TryTargetSystem(out TargetSystem* instance)
        => Resolved(instance = TargetSystem.StaticAddressPointers.pInstance, "TargetSystem.Instance");

    // ── 靜態位址是「存放指標的格子」:解出來之後,遊戲還沒建立該物件時仍是 null ──

    public static CSFramework* FrameworkOrNull()
    {
        var slot = CSFramework.StaticAddressPointers.ppInstance;
        return Resolved(slot, "Framework.Instance") ? *slot : null;
    }

    public static AtkStage* AtkStageOrNull()
    {
        var slot = AtkStage.StaticAddressPointers.ppInstance;
        return Resolved(slot, "AtkStage.Instance") ? *slot : null;
    }

    public static MJIManager* MJIManagerOrNull()
    {
        var slot = MJIManager.StaticAddressPointers.ppInstance;
        return Resolved(slot, "MJIManager.Instance") ? *slot : null;
    }

    // ── CS 裡「自己不擲、往下會擲」的取得器所靠的位址 ──

    /// <summary><c>UIModule.Instance()</c> 與架在它上面的模組取得器(<c>RaptureAtkUnitManager.Instance()</c> 等)要靠的位址都解出了。</summary>
    public static bool UIModuleResolved()
        => Resolved(CSFramework.StaticAddressPointers.ppInstance, "Framework.Instance")
           && Resolved(CSFramework.MemberFunctionPointers.GetUIModule, "Framework.GetUIModule");

    /// <summary>產生器產出的 <c>AgentX.Instance()</c> 要靠的位址都解出了。</summary>
    public static bool AgentChainResolved()
        => UIModuleResolved() && Resolved(AgentModule.MemberFunctionPointers.GetAgentByInternalId, "AgentModule.GetAgentByInternalId");

    /// <summary>產生器產出的 <c>InfoProxyX.Instance()</c> 要靠的位址都解出了。</summary>
    public static bool InfoProxyChainResolved()
        => UIModuleResolved() && Resolved(InfoModule.MemberFunctionPointers.GetInfoProxyById, "InfoModule.GetInfoProxyById");

    private static void ReportOnce(string name)
    {
        name ??= "";
        if(!Reported.TryAdd(name, 0)) return;
        // 同一個位址在各呼叫點的寫法不同(有沒有帶命名空間):收斂成「型別.函式」再去重,log 才是一個位址一行。
        var canonical = Canonical(name);
        if(canonical != name && !Reported.TryAdd(canonical, 0)) return;
        name = canonical;
        try
        {
            PluginLog.Error($"[ECommons] FFXIVClientStructs 位址 {name} 沒有解出(遊戲改版後特徵碼對不上?):用到它的功能在插件更新前回報不可用或不執行。(每個位址只記這一次)");
        }
        catch(Exception)
        {
            // 刻意吞掉:記錄失敗(例如 ECommons 還沒初始化)不能把「不擲例外」的查詢變回會擲。
        }
    }

    /// <summary><c>…Framework.MemberFunctionPointers.GetUIModule</c> → <c>Framework.GetUIModule</c>;其他形狀原樣回傳。</summary>
    internal static string Canonical(string name)
    {
        const string Marker = ".MemberFunctionPointers.";
        var at = name.IndexOf(Marker, StringComparison.Ordinal);
        if(at <= 0) return name;
        var typeStart = name.LastIndexOf('.', at - 1) + 1;
        return name[typeStart..at] + "." + name[(at + Marker.Length)..];
    }
}
