using Dalamud;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Logging;
using System;

namespace ECommons.Automation;
#nullable disable

/// <summary>
/// Provides automatic cutscene skipping trigger. Does not includes cutscene skipping confirmation.
/// </summary>
public unsafe class AutoCutsceneSkipper
{
    private delegate byte CutsceneHandleInputDelegate(nint a1, float a2);
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 79 28 00", DetourName = nameof(CutsceneHandleInputDetour))]
    private static Hook<CutsceneHandleInputDelegate> CutsceneHandleInputHook;

    private static readonly string ConditionSig = "75 11 BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 84 C0 74 4C";

    private static int ConditionOriginalValuesLen => ConditionSig.Split(" ").Length;

    private static nint ConditionAddr;
    /// <summary>
    /// Condition which will be checked to determine if the cutscene should be skipped. Can be null to skip everything unconditionally.
    /// </summary>
    public static Func<nint, bool> Condition;

    /// <summary>
    /// Initializes cutscene skipper trigger. 
    /// </summary>
    /// <param name="cutsceneSkipCondition">Condition which will be checked to determine if the cutscene should be skipped. Can be null to skip everything unconditionally.</param>
    /// <exception cref="Exception">If already initialized</exception>
    public static void Init(Func<nint, bool> cutsceneSkipCondition)
    {
        if(CutsceneHandleInputHook != null) throw new Exception($"{nameof(AutoCutsceneSkipper)} module is already initialized!");
        PluginLog.Information($"AutoCutsceneSkipper requested");
        Condition = cutsceneSkipCondition;
        SignatureHelper.Initialise(new AutoCutsceneSkipper());
        ConditionAddr = Svc.SigScanner.ScanText(ConditionSig);
        PluginLog.Information($"Found cutscene skip condition address at 0x{ConditionAddr:X16}");
        CutsceneHandleInputHook?.Enable();
        PluginLog.Information($"AutoCutsceneSkipper initialized");
    }

    /// <summary>
    /// Disables cutscene skipper trigger. Note that you do not need to call this in Dispose of the plugin, it is disposed automatically.
    /// </summary>
    public static void Disable() => CutsceneHandleInputHook.Disable();
    /// <summary>
    /// Enables previously disabled cutscene trigger. Note that you do not have to call this in constructor of the plugin, it is enabled automatically.
    /// </summary>
    public static void Enable() => CutsceneHandleInputHook.Enable();

    internal static void Dispose()
    {
        CutsceneHandleInputHook?.Dispose();
    }

    /// <remarks>
    /// 🔴 這個 detour 會<b>暫時改寫遊戲自身的機器碼</b>(把條件跳轉 <c>0x75</c> 換成無條件跳轉
    /// <c>0xEB</c>),呼叫完原函式再寫回去。所以「寫回去」這一步<b>必須</b>是無論如何都會執行的:
    /// <list type="bullet">
    /// <item>還原若沒跑到,<c>0xEB</c> 就<b>永久</b>留在遊戲碼上 —— 此後每一段過場都被無條件跳過,
    /// 而且沒有任何錯誤徵兆、log 也不會留痕跡,直到重開遊戲為止。</item>
    /// <item>原寫法把還原放在 try 區塊的<b>最後一行</b>:<c>Original</c> 一擲例外就跳去 catch,
    /// 還原整行被跳過。</item>
    /// </list>
    /// ⚠️ 這裡<b>只</b>保證還原會執行,patch 的語意(改哪個位址、寫哪個位元組)完全沒動。
    /// </remarks>
    internal static byte CutsceneHandleInputDetour(nint a1, float a2)
    {
        if(!Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent])
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
                    SafeMemory.WriteBytes(ConditionAddr, [0xEB]);
                    try
                    {
                        // 🔴 called 必須在呼叫「之前」就設起來。原本設在 Original 回傳之後,
                        // 於是 Original 一擲例外 called 就停在 false,底下的補呼叫會對
                        // 同一幀的同一份輸入「再跑一次」遊戲的過場輸入處理 —— 而且那一次
                        // 在 try 之外,再擲一次就直接穿出 detour 進原生層。
                        // 語意上 called 要表達的是「Original 已經被呼叫過(不管結果如何)」。
                        called = true;
                        ret = CutsceneHandleInputHook.OriginalDisposeSafe(a1, a2);
                    }
                    finally
                    {
                        // 🔴 還原一定要在 finally,理由見上面的 remarks。
                        SafeMemory.WriteBytes(ConditionAddr, [0x75]);
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
}
