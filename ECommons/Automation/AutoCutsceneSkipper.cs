using Dalamud;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Logging;
using System;
using System.Threading;

namespace ECommons.Automation;
#nullable disable

/// <summary>
/// Provides automatic cutscene skipping trigger. Does not includes cutscene skipping confirmation.
/// </summary>
public unsafe class AutoCutsceneSkipper
{
    private delegate byte CutsceneHandleInputDelegate(nint a1, float a2);
    private const string HookSig = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 79 28 00";

    /// <summary>null ＝ 沒初始化,或特徵碼對不上而整個模組停用。所有公開入口都要容許它是 null。</summary>
    [Signature(HookSig, DetourName = nameof(CutsceneHandleInputDetour))]
    private static Hook<CutsceneHandleInputDelegate> CutsceneHandleInputHook;

    private static readonly string ConditionSig = "75 11 BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 84 C0 74 4C";

    private static int ConditionOriginalValuesLen => ConditionSig.Split(" ").Length;

    private static nint ConditionAddr;

    /// <summary>
    /// patch 點相對被 hook 函式起點的位移上限(不含)。該函式長 0x1C3、下一支函式起點在 +0x1D0、patch 點在 +0x137:
    /// 小於這個值 ⇒ 還在同一支函式(或它尾端的填充)裡。
    /// </summary>
    private const long ConditionOffsetLimit = 0x1D0;

    /// <summary>未 patch 時該位址上的原始指令:<c>jne</c>(條件跳躍)。</summary>
    private const byte ConditionByteOriginal = 0x75;

    /// <summary>patch 時寫進去的指令:<c>jmp</c>(無條件跳躍)。</summary>
    private const byte ConditionBytePatched = 0xEB;

    /// <summary>
    /// 位元組驗證通過才會是 true。false ＝ 這個模組<b>一個 byte 都不會寫進遊戲</b>
    /// (過場自動跳過從此不作用,但遊戲碼保證乾淨)。
    /// </summary>
    private static bool Armed;

    /// <summary>
    /// 我們寫進了 <see cref="ConditionBytePatched"/> 但還沒確認寫回。
    /// 🔴 這是判斷「現場那個 0xEB 是不是我留下的」<b>唯一</b>依據:同一個函式上會同時掛著
    /// 別的外掛的同一份模組(AutoRetainer 與 TextAdvance 都會呼叫 Init),
    /// 而每個外掛各自載入自己的 ECommons,靜態欄位彼此看不見。
    /// 無條件把現場的 EB 修回去,會把別的外掛正在使用中的 patch 靜默取消掉。
    /// </summary>
    private static bool OutstandingPatch;

    /// <summary>本外掛 detour 的巢狀深度。0 ＝ 沒有任何一層正在執行。</summary>
    private static int Depth;

    /// <summary>最後一次進入 detour 的時刻,用來偵測 <see cref="Depth"/> 是否已經卡死。</summary>
    private static long LastEntryTicks;

    private static bool LoggedLeak;

    /// <summary>
    /// Condition which will be checked to determine if the cutscene should be skipped. Can be null to skip everything unconditionally.
    /// </summary>
    public static Func<nint, bool> Condition;

    /// <summary>
    /// Initializes cutscene skipper trigger. 
    /// </summary>
    /// <param name="cutsceneSkipCondition">Condition which will be checked to determine if the cutscene should be skipped. Can be null to skip everything unconditionally.</param>
    /// <exception cref="Exception">If already initialized</exception>
    /// <remarks>特徵碼對不上(遊戲改版)時不擲例外:記一則 Warning,過場自動跳過停用,呼叫端照常往下載入。</remarks>
    public static void Init(Func<nint, bool> cutsceneSkipCondition)
    {
        if(CutsceneHandleInputHook != null) throw new Exception($"{nameof(AutoCutsceneSkipper)} module is already initialized!");
        PluginLog.Information($"AutoCutsceneSkipper requested");
        Condition = cutsceneSkipCondition;
        if(!TryResolve()) return;
        PluginLog.Information($"Found cutscene skip condition address at 0x{ConditionAddr:X16}");
        var hookAddr = CutsceneHandleInputHook.Address;
        var offset = (long)ConditionAddr - (long)hookAddr;
        if(!IsConditionOffsetInRange(offset))
        {
            // 不變式:patch 點在被 hook 的函式裡。不成立時殘留修復的前提不存在,一個 byte 都不寫。
            Armed = false;
            PluginLog.Warning(
                $"AutoCutsceneSkipper: 「過場跳過條件」(0x{ConditionAddr:X16})不在被 hook 的函式(起點 0x{hookAddr:X16})裡:" +
                $"兩者相差 {offset} bytes,允許範圍是 1 到 {ConditionOffsetLimit - 1}。已停止改寫遊戲碼,過場自動跳過停用");
        }
        else
        {
            // 🔴 寫進遊戲碼之前先確認現場真的是我們以為的那條指令。特徵碼第一個 byte 就是 75,
            // 所以命中必然是 jne —— 但「命中的是不是同一個 jne」離線證不到,遊戲改版時
            // 這個檢查是唯一擋得住盲寫的東西。驗不過就整個模組停手,不是照樣寫下去。
            Armed = TryReadConditionByte(out var initial) && initial == ConditionByteOriginal;
            if(Armed)
            {
                PluginLog.Information($"AutoCutsceneSkipper: 目標指令驗證通過(0x{ConditionAddr:X16} = 0x{initial:X2})");
            }
            else
            {
                Disarm($"目標指令驗證失敗(讀到 0x{initial:X2})");
            }
        }
        CutsceneHandleInputHook?.Enable();
        PluginLog.Information($"AutoCutsceneSkipper initialized");
    }

    /// <summary>
    /// 解兩條特徵碼並建立 hook。回 false ＝ 任一條對不上:不留 hook、不留位址、不擲例外。
    /// </summary>
    private static bool TryResolve()
    {
        // 先解 patch 點:hook 一建立就會改寫被 hook 函式的開頭,patch 點對不上時不該留下它。
        if(!Svc.SigScanner.TryScanText(ConditionSig, out var conditionAddr))
        {
            PluginLog.Warning($"AutoCutsceneSkipper: 找不到「過場跳過條件」的特徵碼({ConditionSig}),過場自動跳過已停用");
            return false;
        }
        Exception error = null;
        try
        {
            SignatureHelper.Initialise(new AutoCutsceneSkipper());
        }
        catch(Exception e)
        {
            error = e;
        }
        if(CutsceneHandleInputHook == null)
        {
            PluginLog.Warning($"AutoCutsceneSkipper: 「過場輸入處理函式」的 hook 沒有建立(特徵碼 {HookSig}),過場自動跳過已停用。{error?.GetBaseException().Message}");
            return false;
        }
        ConditionAddr = conditionAddr;
        return true;
    }

    /// <summary>patch 點相對被 hook 函式起點的位移是否落在 (0, <see cref="ConditionOffsetLimit"/>) 內。</summary>
    internal static bool IsConditionOffsetInRange(long offset) => offset > 0 && offset < ConditionOffsetLimit;

    /// <summary>
    /// Disables cutscene skipper trigger. Note that you do not need to call this in Dispose of the plugin, it is disposed automatically.
    /// </summary>
    public static void Disable()
    {
        CutsceneHandleInputHook?.Disable();
        // 🔴 hook 一關,這個函式就不再經過我們的 detour ——
        // 殘留的 EB 從此沒有任何一條路徑會修它,所以在這裡收。
        HealIfLeaked();
    }
    /// <summary>
    /// Enables previously disabled cutscene trigger. Note that you do not have to call this in constructor of the plugin, it is enabled automatically.
    /// </summary>
    public static void Enable() => CutsceneHandleInputHook?.Enable();

    internal static void Dispose()
    {
        // 🔴 順序不能反:先把殘留修回去,再拆 hook。
        HealIfLeaked();
        CutsceneHandleInputHook?.Dispose();
    }

    /// <remarks>
    /// 🔴 這個 detour 會<b>暫時改寫遊戲自身的機器碼</b>(把條件跳轉 <c>0x75</c> 換成無條件跳轉
    /// <c>0xEB</c>),呼叫完原函式再寫回去。還原若沒跑到,<c>0xEB</c> 就<b>永久</b>留在遊戲碼上:
    /// 此後每一段過場都被無條件跳過、沒有任何徵兆,直到重開遊戲為止。
    /// <para>
    /// 還原有三層保證,都不靠例外處理:①<b>寫之前先讀</b>:現場不是 0x75 就不寫(是 0xEB 代表別人
    /// 正在用,不寫也不接手還原責任;是別的值代表特徵碼對不上,整個模組停手)。
    /// ②<b>寫回之後再讀一次確認</b>:<c>SafeMemory.WriteBytes</c> 回 bool,寫回失敗要看得見。
    /// </para>
    /// <para>
    /// ③<b>最外層進出時修殘留</b>(<see cref="HealIfLeaked"/>)。前提是 patch 點落在被 hook 的這個函式
    /// <b>裡面</b>(<see cref="Init"/> 已驗,不成立就不寫):殘留的 EB 只有在這個函式再次執行時才會生效,而那必然先經過這裡。
    /// </para>
    /// </remarks>
    internal static byte CutsceneHandleInputDetour(nint a1, float a2)
    {
        var now = Environment.TickCount64;
        var depth = Interlocked.Increment(ref Depth);
        if(depth > 1 && now - LastEntryTicks > 5000)
        {
            // 上一層進入超過 5 秒還沒退出 ⇒ 它的 finally 沒跑到(受管理例外穿過 hook 的
            // 原生 trampoline 時,執行期不保證能正確解開堆疊)。真正的巢狀只有幾微秒,
            // 5 秒是極保守的門檻。不把深度歸位的話,「最外層」永遠不成立,
            // 底下的殘留修復就整個失效 —— 那正是我們要防的那個永久故障。
            Interlocked.Exchange(ref Depth, 1);
            depth = 1;
        }
        LastEntryTicks = now;
        try
        {
            if(depth == 1) HealIfLeaked();
            return CutsceneHandleInputCore(a1, a2);
        }
        finally
        {
            if(depth == 1) HealIfLeaked();
            Interlocked.Decrement(ref Depth);
        }
    }

    private static byte CutsceneHandleInputCore(nint a1, float a2)
    {
        // 沒通過驗證(Armed=false)時純轉呼叫、不讀 a1:被 hook 的可能已經不是原來那支函式。
        if(!Armed || !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent])
        {
            return CutsceneHandleInputHook.OriginalDisposeSafe(a1, a2);
        }
        var called = false;
        byte ret = 0;
        try
        {
            if(Condition?.Invoke(a1) != false)
            {
                var skippable = *(nint*)(a1 + 56) != 0;
                if(skippable)
                {
                    // 🔴 只有「這一層真的寫了 EB」才由這一層負責寫回去。
                    // 現場已經是 EB 時 TryApplyPatch 回 false —— 那是別的外掛寫的,誰寫的誰還原。
                    var owned = TryApplyPatch();
                    try
                    {
                        // 🔴 called 必須在呼叫「之前」就設起來:它表達的是「Original 已經被呼叫過(不管結果如何)」。
                        // 設在回傳之後的話,Original 一擲例外底下的補呼叫就會對同一幀的同一份輸入再跑一次,
                        // 而且那一次在 try 之外,再擲就直接穿出 detour 進原生層。
                        called = true;
                        ret = CutsceneHandleInputHook.OriginalDisposeSafe(a1, a2);
                    }
                    finally
                    {
                        // 🔴 還原一定要在 finally,理由見 CutsceneHandleInputDetour 的 remarks。
                        if(owned) RestorePatch();
                    }
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        if(!called)
        {
            ret = CutsceneHandleInputHook.OriginalDisposeSafe(a1, a2);
        }
        return ret;
    }

    /// <summary>
    /// 讀回 <see cref="ConditionAddr"/> 上的那個 byte。
    /// 📌 走 ReadProcessMemory 而不是直接解參考:位址壞掉時它是回 false,
    /// 不是擲 AccessViolationException(那個在 .NET Core 攔不到)。
    /// </summary>
    private static bool TryReadConditionByte(out byte value)
    {
        value = 0;
        if(ConditionAddr == nint.Zero) return false;
        if(!SafeMemory.ReadBytes(ConditionAddr, 1, out var buffer) || buffer.Length != 1) return false;
        value = buffer[0];
        return true;
    }

    /// <summary>停止一切對遊戲碼的寫入,並留下一行看得到的 Information。</summary>
    private static void Disarm(string reason)
    {
        Armed = false;
        PluginLog.Information(
            $"AutoCutsceneSkipper: {reason}(位址 0x{ConditionAddr:X16}),已停止改寫遊戲碼以保護遊戲。" +
            "過場自動跳過從此不作用;重新啟動遊戲即可恢復。");
    }

    /// <summary>
    /// 嘗試把條件跳躍改成無條件跳躍。
    /// </summary>
    /// <returns>true ＝ <b>這一層</b>寫了 patch,必須由這一層寫回去。</returns>
    private static bool TryApplyPatch()
    {
        if(!Armed) return false;
        if(!TryReadConditionByte(out var current))
        {
            Disarm("讀取目標指令失敗");
            return false;
        }
        if(current == ConditionBytePatched)
        {
            // 已經是 EB:別的外掛(或我們的外層)正在用。不重複寫,也不接手還原責任。
            return false;
        }
        if(current != ConditionByteOriginal)
        {
            Disarm($"目標指令與預期不符,現場是 0x{current:X2}");
            return false;
        }
        // 🔴 旗標必須在寫入「之前」設起來。「寫進去了卻沒記錄下來」是唯一會讓 EB
        // 永久留在遊戲碼上而且沒有任何一條路徑知道要去修它的組合。
        // 反過來(記錄了但其實沒寫成功)只會多做一次無害的確認讀取。
        OutstandingPatch = true;
        if(SafeMemory.WriteBytes(ConditionAddr, [ConditionBytePatched])) return true;
        OutstandingPatch = false;
        Disarm("寫入失敗(WriteProcessMemory 回 false)");
        return false;
    }

    /// <summary>
    /// 把我們寫進去的 patch 寫回原始指令,<b>並讀回來確認真的寫回去了</b>。
    /// </summary>
    private static void RestorePatch()
    {
        if(!OutstandingPatch) return;
        // 🔴 WriteBytes 的回傳值一定要看:寫回失敗時沒有例外、沒有 log、沒有任何徵兆,
        // 而遊戲從此每一段過場都被無條件跳過。
        if(SafeMemory.WriteBytes(ConditionAddr, [ConditionByteOriginal])
            && TryReadConditionByte(out var current)
            && current == ConditionByteOriginal)
        {
            OutstandingPatch = false;
            return;
        }
        // 還原沒成功:旗標留著,最外層進出時的 HealIfLeaked 會再試一次。
        PluginLog.Information(
            $"AutoCutsceneSkipper: 還原 0x{ConditionAddr:X16} 未確認成功,將在下次進出 detour 時重試");
    }

    /// <summary>
    /// 修掉「我們寫了但沒還原成功」而殘留在遊戲碼上的 0xEB。
    /// <para>
    /// 🔴 <b>只修我們自己寫的</b>(<see cref="OutstandingPatch"/>)。現場的 EB 也可能是
    /// 另一個外掛正在使用中的 patch —— 每個外掛各自載入自己的 ECommons,靜態欄位互相看不見,
    /// 所以「現場是 EB 就修回去」會把別人的過場跳過靜默取消掉(各消費端的啟用條件互相獨立,
    /// 兩邊意見不同是常態)。
    /// </para>
    /// <para>
    /// 📌 別的外掛留下的殘留由它自己的這個方法收:兩邊都掛在同一個函式上,
    /// 每次呼叫兩邊的 detour 都會跑到,而 patch 點就在這個函式裡面,
    /// 所以殘留一定會在它真正生效之前被原主人修掉。
    /// </para>
    /// </summary>
    private static void HealIfLeaked()
    {
        if(!OutstandingPatch) return;
        if(!TryReadConditionByte(out var current)) return;
        if(current != ConditionBytePatched)
        {
            OutstandingPatch = false;
            return;
        }
        if(!SafeMemory.WriteBytes(ConditionAddr, [ConditionByteOriginal])) return;
        OutstandingPatch = false;
        if(LoggedLeak) return;
        LoggedLeak = true;
        PluginLog.Information(
            $"AutoCutsceneSkipper: 偵測到 0x{ConditionAddr:X16} 殘留 0x{ConditionBytePatched:X2} 並已修回 0x{ConditionByteOriginal:X2}");
    }
}
