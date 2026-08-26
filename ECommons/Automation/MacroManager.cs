using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ECommons.Automation;
#nullable disable

public static unsafe class MacroManager
{
    public static void Execute(string multilineString)
    {
        Execute(multilineString.Replace("\r", "").Split("\n"));
    }

    public static void Execute(params string[] commands)
    {
        Execute((IEnumerable<string>)commands);
    }

    public static void Execute(IEnumerable<string> commands)
    {
        var macroPtr = IntPtr.Zero;
        GenericHelpers.Safe(delegate
        {
            var count = (byte)Math.Max(Macro.numLines, commands.Count());
            if(count > Macro.numLines)
            {
                throw new InvalidOperationException("Macro was more than 15 lines!");
            }
            if(commands.Any(x => x.Length > 180))
            {
                throw new InvalidOperationException("Macro contained lines more than 180 symbols!");
            }
            if(commands.Any(x => x.Contains("\n") || x.Contains("\r") || x.Contains("\0") || Chat.SanitiseText(x).Length != x.Length))
            {
                throw new InvalidOperationException("Macro contained invalid symbols!");
            }
            macroPtr = Marshal.AllocHGlobal(Macro.size);
            using var macro = new Macro(macroPtr, string.Empty, commands.ToArray());
            Marshal.StructureToPtr(macro, macroPtr, false);

            // 🔴 RaptureShellModule.Instance() 是手寫取得器(UIModule.Instance() 為 null 就回 null)。
            // ExecuteMacro 是原生成員函式,對 null 的 this 呼叫會在遊戲碼裡解參考 ＝ AccessViolation,
            // 而 AVE 是 corrupted-state exception,外層的 GenericHelpers.Safe 也攔不到。
            // 擲一般例外則會被 Safe 攔下並記錄,巨集不執行;macroPtr 的釋放在 Safe 之外,不受影響。
            var shell = RaptureShellModule.Instance();
            if(shell == null) throw new InvalidOperationException("RaptureShellModule is not available; macro was not executed.");
            shell->ExecuteMacro((FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule.Macro*)macroPtr);
        });

        Marshal.FreeHGlobal(macroPtr);
    }

}
