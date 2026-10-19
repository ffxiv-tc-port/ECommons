using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;

namespace ECommons.ExcelServices;
#nullable disable

public static unsafe class ExcelActionHelper
{
    /// <summary>
    /// 取得技能的剩餘冷卻秒數。<paramref name="id"/> 在 Action 表裡不存在時回 <c>0</c>(視同不在冷卻)。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>GetRow</c> 對不存在的列是<b>擲例外</b>不是回 null,而這個函式吃的是呼叫端傳進來的動態 id
    /// (台服的 Action 表跟國際服不一定同步,寫死的 id 在這裡是靜默的錯誤來源)。
    /// 所以用 <c>GetRowOrDefault</c> 並明確走 null 分支。
    /// <c>GetRecastGroupDetail</c> 回 null 時同樣當成不在冷卻:那是原生指標,直接解參考是
    /// AccessViolation(try/catch 攔不到)。
    /// <para>
    /// <c>ActionManager</c> 或 <c>GetRecastGroupDetail</c> 的位址沒解出(遊戲改版)時回 <see cref="float.MaxValue"/>(＝讀不到,當成還在冷卻):
    /// 回 0 會讓「冷卻好了就用」的呼叫端誤以為可以用。
    /// </para>
    /// </remarks>
    public static float GetActionCooldown(uint id)
    {
        var row = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id);
        if(row == null) return 0;
        if(!CSAddress.TryActionManager(out var manager) || !CSAddress.Resolved(ActionManager.MemberFunctionPointers.GetRecastGroupDetail)) return float.MaxValue;

        var detail = manager->GetRecastGroupDetail(row.Value.CooldownGroup - 1);
        var cdg2 = row.Value.AdditionalCooldownGroup - 1;
        var ret = detail != null && detail->IsActive ? detail->Total - detail->Elapsed : 0;
        if(cdg2 > 0)
        {
            var detail2 = manager->GetRecastGroupDetail(cdg2);
            var cd2 = detail2 != null && detail2->IsActive ? detail2->Total - detail2->Elapsed : 0;
            return Math.Max(cd2, ret);
        }
        return ret;
    }

    public static string GetActionName(this Lumina.Excel.Sheets.Action? dataNullable, bool forceIncludeID = false)
    {
        if(dataNullable == null)
        {
            return $"null";
        }
        else
        {
            var name = dataNullable?.Name.GetText();
            if(name.IsNullOrEmpty())
            {
                return $"#{dataNullable.Value.RowId}";
            }
            else
            {
                return (forceIncludeID ? $"#{dataNullable.Value.RowId} " : $"") + name;
            }
        }
    }

    public static string GetActionName(uint id, bool forceIncludeID = false)
    {
        var d = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id);
        if(d == null) return $"#{id}";
        return d.GetActionName(forceIncludeID);
    }
}
