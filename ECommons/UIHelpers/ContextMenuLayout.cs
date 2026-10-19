using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ECommons.UIHelpers;

/// <summary>
/// 右鍵選單（"ContextMenu" addon）AtkValues 的版面：第一個項目在哪一格（7.25 是 7、7.4 世代是 8）。
/// </summary>
/// <remarks>
/// 從遊戲碼推導，不寫死；第一次查詢時才掃描，結果整個插件生命期共用。
/// 推不出來時 <see cref="TryGetFirstEntryIndex"/> 回 <see langword="false"/>：呼叫端必須停用依賴項目位置的功能
/// （讀錯一格＝文字對到下一個選項），登入後在聊天視窗提示一次。
/// </remarks>
public static unsafe class ContextMenuLayout
{
    private static readonly Lazy<int> FirstEntry = new(Derive, LazyThreadSafetyMode.ExecutionAndPublication);
    private static int PendingEcho;

    /// <summary>第一個項目的 AtkValues 索引；項目 i 在 <c>index + i</c>。推不出來時回 <see langword="false"/>。</summary>
    public static bool TryGetFirstEntryIndex(out int index)
    {
        index = FirstEntry.Value;
        if(index != ContextMenuLayoutDerivation.NotDerived) return true;
        if(Volatile.Read(ref PendingEcho) == 1 && Svc.ClientState.IsLoggedIn && Interlocked.Exchange(ref PendingEcho, 0) == 1)
        {
            Svc.Chat.PrintError($"[{Svc.PluginInterface.Manifest.Name}] 推不出右鍵選單的版面（遊戲改版？），這個插件用到右鍵選單的自動點選先停用，等插件更新。");
        }
        return false;
    }

    private static int Derive()
    {
        int index;
        string evidence;
        if(sizeof(AtkValue) != ContextMenuLayoutDerivation.AtkValueSize)
        {
            index = ContextMenuLayoutDerivation.NotDerived;
            evidence = $"sizeof(AtkValue)={sizeof(AtkValue)}";
        }
        else
        {
            index = ContextMenuLayoutDerivation.Derive(ReadMatch, out evidence);
        }
        if(index == ContextMenuLayoutDerivation.NotDerived)
        {
            PluginLog.Error($"[ECommons] 推不出右鍵選單第一個項目在 AtkValues 的哪一格，用到它的功能停用（{evidence}）");
            Volatile.Write(ref PendingEcho, 1);
        }
        else
        {
            PluginLog.Information($"[ECommons] 右鍵選單第一個項目在 AtkValues[{index}]（{evidence}）");
        }
        return index;
    }

    // 讀 SigScanner 的模組副本，命中點與長度都要落在 .text 內。
    private static byte[]? ReadMatch(string signature, int length)
    {
        var scanner = Svc.SigScanner;
        if(!scanner.TryScanText(signature, out var address)) return null;
        var rva = (long)address - (long)scanner.Module.BaseAddress;
        if(rva < scanner.TextSectionOffset || rva + length > scanner.TextSectionOffset + scanner.TextSectionSize) return null;
        var bytes = new byte[length];
        Marshal.Copy(scanner.SearchBase + (nint)rva, bytes, 0, length);
        return bytes;
    }
}
