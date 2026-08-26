using ECommons.DalamudServices;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ECommons.ExcelServices;
#nullable disable

public static class ExcelJobHelper
{
    public static readonly ReadOnlyDictionary<Job, Job> Upgrades = new Dictionary<Job, Job>()
    {
        [Job.GLA] = Job.PLD,
        [Job.PGL] = Job.MNK,
        [Job.MRD] = Job.WAR,
        [Job.LNC] = Job.DRG,
        [Job.ARC] = Job.BRD,
        [Job.CNJ] = Job.WHM,
        [Job.THM] = Job.BLM,
        [Job.ACN] = Job.SMN,
        [Job.ROG] = Job.NIN,
    }.AsReadOnly();

    public static Job GetUpgradedJob(this Job j)
    {
        if(Upgrades.TryGetValue(j, out var job)) return job;
        return j;
    }

    public static Job GetDowngradedJob(this Job j)
    {
        var dj = Upgrades.FindKeysByValue(j);
        if(dj.TryGetFirst(out var ret))
        {
            return ret;
        }
        return j;
    }

    public static bool IsUpgradeable(this Job j) => Upgrades.ContainsKey(j);

    public static bool IsCombat(this Job j) => j.GetData().Role > 0;

    public static bool IsDol(this Job j) => j.GetData().ClassJobCategory.RowId == 32;
    public static bool IsDoh(this Job j) => j.GetData().ClassJobCategory.RowId == 33;
    public static bool IsDom(this Job j) => j.GetData().ClassJobCategory.RowId == 31;
    public static bool IsDow(this Job j) => j.GetData().ClassJobCategory.RowId == 30;

    public static bool IsTank(this Job j) => j.GetData().Role == 1;
    public static bool IsHealer(this Job j) => j.GetData().Role == 4;
    public static bool IsDps(this Job j) => j.IsMeleeDps() || j.IsRangedDps();
    public static bool IsMeleeDps(this Job j) => j.GetData().Role == 2;
    public static bool IsRangedDps(this Job j) => j.GetData().Role == 3;
    public static bool IsPhysicalRangedDps(this Job j) => j.IsRangedDps() && j.IsDow();
    public static bool IsMagicalRangedDps(this Job j) => j.IsRangedDps() && j.IsDom();

    /// <summary>
    /// 取得職業的 <c>ClassJob</c> 列。查不到時回<b>第 0 列(冒險者/ADV)</b>而不是擲例外。
    /// 要區分「真的是冒險者」與「查不到」請改用 <see cref="GetDataOrDefault"/>。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>GetRow</c> 對不存在的列<b>擲例外</b>不回 null。這個函式是本檔十幾個
    /// <c>IsTank</c>/<c>IsHealer</c>/<c>IsDol</c>… 的共同底層,而那些多半跑在 ImGui 繪製迴圈或
    /// LINQ 排序鍵裡 —— 擲一次例外就是整個視窗不畫,而且 <see cref="Job"/> 是可以從任意 uint
    /// 轉型過來的(遊戲新增職業而列舉還沒補時就會落在表外)。
    /// <br/><br/>
    /// 📌 退路選第 0 列而不是 <c>default(ClassJob)</c>:Lumina 的列結構是<b>對資料頁的參照</b>,
    /// <c>default</c> 的頁是 null,讀任何欄位都會 NullReferenceException —— 那只是把例外從這裡
    /// 搬到呼叫端。第 0 列是「冒險者」,<c>Role == 0</c>、各職業旗標全 false,
    /// 讓 <c>IsCombat</c>/<c>IsTank</c> 這些述詞得到「都不是」這個保守答案。
    /// (2026-08-06 離線確認台服 7.20 的 ClassJob 表有 46 列、含第 0 列。)
    /// </remarks>
    public static ClassJob GetData(this Job j)
    {
        var row = GetDataOrDefault(j);
        if(row != null) return row.Value;

        LogUnknownJobOnce((uint)j);
        // 第 0 列理論上必定存在;真的連它都沒有就只剩 default,行為與加固前等價(呼叫端會拿到例外)。
        return Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(0) ?? default;
    }

    /// <summary>
    /// 同 <see cref="GetData"/>,但查不到時回 <see langword="null"/>,讓呼叫端自己決定怎麼處理未知職業。
    /// </summary>
    public static ClassJob? GetDataOrDefault(this Job j) => Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault((uint)j);

    private static readonly HashSet<uint> ReportedUnknownJobs = [];

    /// <summary>
    /// 同一個未知職業 id 只記一次 —— 呼叫端多半在每幀迴圈裡,不設閘門會把 log 灌爆。
    /// 用 <c>Warning</c> 等級是為了讓跑 LogLevel 2 的使用者看得到(這是要請人回報的診斷)。
    /// </summary>
    private static void LogUnknownJobOnce(uint id)
    {
        lock(ReportedUnknownJobs)
        {
            if(!ReportedUnknownJobs.Add(id)) return;
        }
        PluginLog.Warning($"[ECommons] ClassJob 表裡沒有第 {id} 列,{nameof(GetData)} 退回第 0 列(冒險者)。職業判斷會全部回 false。");
    }

    public static Job GetJob(this ClassJob cj)
    {
        return (Job)cj.RowId;
    }

    public static int GetIcon(this Job j)
    {
        return j == Job.ADV ? 62143 : (062100 + (int)j);
    }

    public static ClassJob? GetJobByName(string name)
    {
        if(Svc.Data.GetExcelSheet<ClassJob>().TryGetFirst(x => x.Name.ToString().EqualsIgnoreCase(name), out var result))
        {
            return result;
        }
        return null;
    }

    public static bool TryGetJobByName(string name, out ClassJob result)
    {
        var r = GetJobByName(name);
        result = r ?? default;
        return r != null;
    }

    public static ClassJob? GetJobByAbbreviation(string name)
    {
        if(Svc.Data.GetExcelSheet<ClassJob>().TryGetFirst(x => x.Abbreviation.ToString().EqualsIgnoreCase(name), out var result))
        {
            return result;
        }
        return null;
    }

    public static bool TryGetJobByAbbreviation(string name, out ClassJob result)
    {
        var r = GetJobByAbbreviation(name);
        result = r ?? default;
        return r != null;
    }

    public static ClassJob? GetJobById(uint id)
    {
        return Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(id);
    }

    public static bool TryGetJobById(uint id, out ClassJob result)
    {
        var r = GetJobById(id);
        result = r ?? default;
        return r != null;
    }

    public static string GetJobNameById(uint id)
    {
        return GetJobById(id)?.Name.ToString();
    }

    public static ClassJob[] GetCombatJobs()
    {
        return Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Role.EqualsAny<byte>(2, 3)).ToArray();
    }

    public static bool IsJobInCategory(this ClassJobCategory cat, Job job)
    {
        if(job == Job.ADV && cat.ADV) return true;
        if(job == Job.GLA && cat.GLA) return true;
        if(job == Job.PGL && cat.PGL) return true;
        if(job == Job.MRD && cat.MRD) return true;
        if(job == Job.LNC && cat.LNC) return true;
        if(job == Job.ARC && cat.ARC) return true;
        if(job == Job.CNJ && cat.CNJ) return true;
        if(job == Job.THM && cat.THM) return true;
        if(job == Job.CRP && cat.CRP) return true;
        if(job == Job.BSM && cat.BSM) return true;
        if(job == Job.ARM && cat.ARM) return true;
        if(job == Job.GSM && cat.GSM) return true;
        if(job == Job.LTW && cat.LTW) return true;
        if(job == Job.WVR && cat.WVR) return true;
        if(job == Job.ALC && cat.ALC) return true;
        if(job == Job.CUL && cat.CUL) return true;
        if(job == Job.MIN && cat.MIN) return true;
        if(job == Job.BTN && cat.BTN) return true;
        if(job == Job.FSH && cat.FSH) return true;
        if(job == Job.PLD && cat.PLD) return true;
        if(job == Job.MNK && cat.MNK) return true;
        if(job == Job.WAR && cat.WAR) return true;
        if(job == Job.DRG && cat.DRG) return true;
        if(job == Job.BRD && cat.BRD) return true;
        if(job == Job.WHM && cat.WHM) return true;
        if(job == Job.BLM && cat.BLM) return true;
        if(job == Job.ACN && cat.ACN) return true;
        if(job == Job.SMN && cat.SMN) return true;
        if(job == Job.SCH && cat.SCH) return true;
        if(job == Job.ROG && cat.ROG) return true;
        if(job == Job.NIN && cat.NIN) return true;
        if(job == Job.MCH && cat.MCH) return true;
        if(job == Job.DRK && cat.DRK) return true;
        if(job == Job.AST && cat.AST) return true;
        if(job == Job.SAM && cat.SAM) return true;
        if(job == Job.RDM && cat.RDM) return true;
        if(job == Job.BLU && cat.BLU) return true;
        if(job == Job.GNB && cat.GNB) return true;
        if(job == Job.DNC && cat.DNC) return true;
        if(job == Job.RPR && cat.RPR) return true;
        if(job == Job.SGE && cat.SGE) return true;
        if(job == Job.VPR && cat.VPR) return true;
        if(job == Job.PCT && cat.PCT) return true;
        return false;
    }
}
