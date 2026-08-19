using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;

namespace ECommons.Automation.NeoTaskManager.Tasks;
public unsafe class FrameDelayTask
{
    private long? StopAt;
    internal TaskManagerTask Task;
    public FrameDelayTask(int ms, TaskManagerConfiguration? configuration = null)
    {
        var config = new TaskManagerConfiguration(abortOnTimeout: false);
        if(configuration != null)
        {
            config = config.With(configuration);
        }
        Task = new((Func<bool?>)(() =>
        {
            // 🔴 Framework.Instance() 宣告成 [StaticAddress("48 8B 1D ?? ?? ?? ?? 8B 7C 24 64", 3, isPointer: true)],
            //    回的是「靜態位址裡存放的那個指標」—— 產生器只在特徵碼失配時擲例外,對取回的值不判空。
            //    登入前、登出後、關閉流程中它真的會是 null。
            //    這個 lambda 是每幀執行的 task 基建(全艦隊所有 NeoTaskManager 使用者都會經過它),
            //    裸解參考 ->FrameCounter 的失敗形式是 AccessViolation —— 在 .NET Core 是
            //    corrupted-state exception,TaskManager 外層的 try/catch(Exception) 完全攔不到,直接把遊戲帶走。
            // 取不到時的語意刻意選 false ——「這一幀沒有推進,下一幀再試」:
            //    ・回 true 會被 TaskManager 當成「延遲已結束」而放行後面的 task,但幀數根本沒走完,那是回退行為。
            //    ・回 null 在 TaskManager 裡是 abort 請求(會清掉整條佇列),對「暫時拿不到 Framework」過度反應。
            //    同時 StopAt 也不會在這一幀被設定,所以延遲的起算點會落在第一個真的拿得到 Framework 的幀。
            // 另外把原本重複兩次的 Framework.Instance() 收成一個區域變數:同一次判斷內只解析一次,
            // 判空與使用看的是同一個指標(原本第二次呼叫理論上可能拿到不同的值)。
            var framework = Framework.Instance();
            if(framework == null) return false;
            StopAt ??= framework->FrameCounter + ms;
            return framework->FrameCounter >= StopAt;
        }), $"Delay ({ms} frames)", config);
    }

    public static implicit operator TaskManagerTask(FrameDelayTask task) => task.Task;
}
