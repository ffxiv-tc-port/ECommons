using Dalamud.Interface.Colors;
using ECommons.ImGuiMethods;
using System.Collections.Generic;

namespace ECommons.Throttlers;
#nullable disable

public class FrameThrottler<T>
{
    private Dictionary<T, long> Throttlers = [];

    /// <summary>幀時鐘。<b>不是</b> <c>UiBuilder.FrameCount</c> —— 那個計數器在過場動畫、使用者隱藏 UI、GPose 期間完全不前進，會讓節流永不到期。詳見 <see cref="FrameThrottlerClock"/>。</summary>
    private static long CurrentFrame => FrameThrottlerClock.CurrentFrame;

    public IReadOnlyCollection<T> ThrottleNames => Throttlers.Keys;

    public bool Throttle(T name, int frames = 60, bool rethrottle = false)
    {
        if(!Throttlers.ContainsKey(name))
        {
            Throttlers[name] = CurrentFrame + frames;
            return true;
        }
        if(CurrentFrame > Throttlers[name])
        {
            Throttlers[name] = CurrentFrame + frames;
            return true;
        }
        else
        {
            if(rethrottle) Throttlers[name] = CurrentFrame + frames;
            return false;
        }
    }

    public void Reset(T name)
    {
        Throttlers.Remove(name);
    }

    public bool Check(T name)
    {
        if(!Throttlers.ContainsKey(name)) return true;
        return CurrentFrame > Throttlers[name];
    }

    public long GetRemainingTime(T name, bool allowNegative = false)
    {
        if(!Throttlers.ContainsKey(name)) return allowNegative ? -CurrentFrame : 0;
        var ret = Throttlers[name] - CurrentFrame;
        if(allowNegative)
        {
            return ret;
        }
        else
        {
            return ret > 0 ? ret : 0;
        }
    }

    public void ImGuiPrintDebugInfo()
    {
        foreach(var x in Throttlers)
        {
            ImGuiEx.Text(Check(x.Key) ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, $"{x.Key}: [{GetRemainingTime(x.Key)} frames remains] ({x.Value})");
        }
    }
}
