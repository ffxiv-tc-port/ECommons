using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ECommons.ExcelServices;
#nullable disable

public static class ExcelWorldHelper
{
    [Obsolete("Please use Get")]
    public static World? GetWorldByName(string name) => Get(name);

    // key 帶 onlyPublic:同一個名稱可能先後以不同的 onlyPublic 呼叫,
    // 若快取鍵不含這個參數,後面的呼叫會拿到前一次呼叫的結果、無視自己要求的篩選條件。
    private static Dictionary<(string Name, bool OnlyPublic), World?> NameCache = [];

    // 台服(陸行鳥 DataCenter=151)現役的 8 個世界,官方 EXD 把 IsPublic 全部填 False
    // (2026-08 台服 7.20 實測,不是資料缺漏,官方本來就這樣填)。只放行這個固定的 RowId
    // 範圍,不放行整個 DataCenter——同一個 DataCenter 底下還混了測試/內部伺服器
    // (例如 4000-4002 測試區、4020/4021/4023/4024 t-/td- 內部環境、4003/4004/4022/
    // 4025-4027/4036-4051 DataCenter=0 的內容伺服器),這些都不應該被視為公開伺服器。
    public static bool IsPublic(this World w)
    {
        if(w.IsPublic) return true;
        return w.RowId.EqualsAny<uint>(4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035);//w.RowId.EqualsAny<uint>(408, 409, 410, 411, 415);
    }

    public static World? Get(string name, bool onlyPublic = false)
    {
        if(name == null) return null;
        var cacheKey = (name, onlyPublic);
        if(NameCache.TryGetValue(cacheKey, out var cached)) return cached;
        World? result = null;
        foreach(var x in Svc.Data.GetExcelSheet<World>())
        {
            if(!x.Name.ToString().EqualsIgnoreCase(name)) continue;
            if(onlyPublic && !x.Region.EqualsAny(Enum.GetValues<Region>().Select(z => (byte)z).ToArray())) continue;
            // 名稱可能撞號:退役/測試伺服器可能跟現役伺服器同名(例如「巴哈姆特」同時存在於
            // 退役 row 1160(DataCenter=0)與台服現役 row 4033)。Excel 是依 RowId 升冪列舉,
            // 退役的舊 row 編號通常比較小,原本的 TryGetFirst 會先命中它。優先採用現役公開
            // 的那一列;若完全沒有公開的候選,才退回原本「第一筆命中」的行為(維持既有相容性)。
            if(x.IsPublic())
            {
                result = x;
                break;
            }
            result ??= x;
        }
        if(result != null) NameCache[cacheKey] = result;
        return result;
    }

    public static World? Get(uint id, bool onlyPublic = false)
    {
        var result = Svc.Data.GetExcelSheet<World>().GetRowOrDefault(id);
        if(result != null && (!onlyPublic || result.Value.Region.EqualsAny(Enum.GetValues<Region>().Select(z => (byte)z).ToArray())))
        {
            return result;
        }
        return null;
    }

    [Obsolete("Please use TryGet")]
    public static bool TryGetWorldByName(string name, out World result) => TryGet(name, out result);

    public static bool TryGet(string name, out World result)
    {
        var r = Get(name);
        result = r ?? default;
        return r != null;
    }

    public static bool TryGet(uint id, out World result)
    {
        var r = Get(id);
        result = r ?? default;
        return r != null;
    }

    public static World[] GetPublicWorlds(Region? region = null)
    {
        return Svc.Data.GetExcelSheet<World>().Where(x => x.IsPublic() && (region == null || x.GetRegion() == region.Value)).ToArray();
    }

    public static World[] GetPublicWorlds(uint dataCenter)
    {
        return Svc.Data.GetExcelSheet<World>().Where(x => x.IsPublic() && x.DataCenter.RowId == dataCenter).ToArray();
    }

    public static WorldDCGroupType[] GetDataCenters(Region? region = null, bool checkForPublicWorlds = false)
    {
        return Svc.Data.GetExcelSheet<WorldDCGroupType>().Where(x => (region == null || (Region)x.Region == region.Value) && (!checkForPublicWorlds || GetPublicWorlds(x.RowId).Length > 0)).ToArray();
    }

    public static WorldDCGroupType[] GetDataCenters(System.Collections.Generic.IEnumerable<Region> regions, bool checkForPublicWorlds = false)
    {
        return Svc.Data.GetExcelSheet<WorldDCGroupType>().Where(x => regions.Contains((Region)x.Region) && (!checkForPublicWorlds || GetPublicWorlds(x.RowId).Length > 0)).ToArray();
    }

    [Obsolete("Please use Get")]
    public static World? GetWorldById(uint id) => Get(id);
    public static World? Get(uint id)
    {
        return Svc.Data.GetExcelSheet<World>().GetRowOrDefault(id);
    }
    public static World? Get(int id) => Get((uint)id);

    [Obsolete("Please use GetName")]
    public static string GetWorldNameById(uint id) => GetName(id);
    public static string GetName(int id) => GetName((uint)id);
    public static string GetName(uint id)
    {
        return Get(id)?.Name.ToString();
    }

    [Obsolete("Please use Get")]
    public static World? GetPublicWorldById(uint id) => Get(id, true);

    [Obsolete("Please use GetName")]
    public static string GetPublicWorldNameById(uint id) => Get(id, true)?.Name.ToString();

    // 這個列舉是公開 API,不能直接加新成員——會動到所有消費端既有的 switch/比對邏輯。
    // 但 WorldDCGroupType.Region 這個底層欄位其實不只 1-4 這四種值:目前已知還有
    // 5=中國大陸、6=韓服、7=NA Cloud DC(Beta)、8=台服(陸行鳥,WorldDCGroupType RowId 151)。
    // GetRegion() 是直接把這個 byte 轉型回來,所以執行期拿得到 (Region)8,只是沒有名字——
    // 任何用 Enum.GetValues&lt;Region&gt;() 迭代地區的 UI 都會漏掉台服(以及中國/韓服)。
    // 需要列出「所有地區」的 UI 請改用 AllRegions() + GetRegionDisplayName()。
    [Obfuscation(Exclude = true)]
    public enum Region
    {
        JP = 1,
        NA = 2,
        EU = 3,
        OC = 4,
    }

    public static Region GetRegion(this World world)
    {
        var dc = world.DataCenter;
        var dcg = Svc.Data.GetExcelSheet<WorldDCGroupType>().GetRowOrDefault(dc.Value.RowId);
        if(dcg == null) return 0;
        return (Region)dcg.Value.Region;
    }

    /// <summary>
    /// 傳回目前資料裡實際出現過的所有地區代碼,依 <c>WorldDCGroupType</c> 表動態取得,
    /// 包含沒有具名列舉值的地區(例如台服=8)。需要迭代/列出「所有地區」的 UI 請用這個,
    /// 不要用 <c>Enum.GetValues&lt;Region&gt;()</c>——那樣會漏掉台服跟其他未命名地區。
    /// </summary>
    public static Region[] AllRegions()
    {
        return [.. Svc.Data.GetExcelSheet<WorldDCGroupType>()
            .Select(x => (Region)x.Region)
            .Where(x => x != 0) // RowId 0 是保留列(Name="Unknown"),不是真的地區
            .Distinct()
            .OrderBy(x => (int)x)];
    }

    /// <summary>
    /// 地區的顯示名稱,涵蓋 <see cref="Region"/> 列舉沒有具名值的地區(中國大陸/韓服/
    /// Cloud/台服)。沒有對應到已知值時退回 <c>(int)region</c> 的數字字串,不會拋例外。
    /// </summary>
    public static string GetRegionDisplayName(this Region region)
    {
        return region switch
        {
            Region.JP => "Japan",
            Region.NA => "North-America",
            Region.EU => "Europe",
            Region.OC => "Oceania",
            (Region)5 => "China",
            (Region)6 => "Korea",
            (Region)7 => "Cloud",
            (Region)8 => "Taiwan",
            _ => ((int)region).ToString(),
        };
    }
}
