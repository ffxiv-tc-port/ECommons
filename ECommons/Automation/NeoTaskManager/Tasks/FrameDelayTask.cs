using ECommons.GameHelpers;
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
            // 🔴 Framework.Instance() 是 [StaticAddress(..., isPointer: true)]:產生器對取回的指標不判空,登入前、
            //    登出後、關閉流程中真的會是 null;裸解參考是 AccessViolation,TaskManager 外層的 try/catch 攔不到。
            // 取不到時刻意回 false(這一幀沒有推進,下一幀再試):回 true 會被當成「延遲已結束」而放行後面的 task,
            //    回 null 在 TaskManager 裡是 abort 請求(會清掉整條佇列)。StopAt 也不會在這一幀被設定,
            //    所以延遲從第一個真的拿得到 Framework 的幀起算。
            var framework = CSAddress.FrameworkOrNull();
            if(framework == null) return false;
            StopAt ??= framework->FrameCounter + ms;
            return framework->FrameCounter >= StopAt;
        }), $"Delay ({ms} frames)", config);
    }

    public static implicit operator TaskManagerTask(FrameDelayTask task) => task.Task;
}
