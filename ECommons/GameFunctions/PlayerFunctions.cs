using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;

namespace ECommons.GameFunctions;

public static unsafe class PlayerFunctions
{
    public static bool TryGetPlaceholder(this IGameObject pc, out int number, bool verbose = false)
    {
        number = default;
        // 🔴 這條鏈三層都可能回 null,而且每一層的失敗方式都是攔不到的 AccessViolation:
        //   ① Framework.Instance() 是 [StaticAddress(..., isPointer: true)],產生器只在特徵碼失配時
        //      擲例外,對回傳值不判空 —— 登入前/關閉中真的會是 null。
        //   ② GetUIModule() 是 [MemberFunction] 原生呼叫,對 null 的 this 呼叫直接在遊戲碼裡解參考。
        //   ③ GetPronounModule() 是 [VirtualFunction(10)],對 null 取 vtable 就是解參考 null。
        // 取不到時回 false(＝這個物件沒有對應的 <1>~<8> 佔位符),與「找遍八個都沒中」的既有回傳一致,
        // 拿得到時的行為完全不變。
        var framework = Framework.Instance();
        if(framework == null) return false;
        var uiModule = framework->GetUIModule();
        if(uiModule == null) return false;
        var pronounModule = uiModule->GetPronounModule();
        if(pronounModule == null) return false;
        for(var i = 1; i <= 8; i++)
        {
            var optr = pronounModule->ResolvePlaceholder($"<{i}>", 0, 0);
            if(verbose) PluginLog.Debug($"Placeholder {i} value {(optr == null ? "null" : optr->EntityId)}");
            if(pc.Address == (IntPtr)optr)
            {
                number = i;
                return true;
            }
        }
        number = default;
        return false;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* BattleChara(this IPlayerCharacter o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)o.Address;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Character(this IPlayerCharacter o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)o.Address;
    }

    public static FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* IGameObject(this IPlayerCharacter o)
    {
        return (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)o.Address;
    }
}
