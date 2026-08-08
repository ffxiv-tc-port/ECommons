using ECommons.Logging;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ECommons.GenericHelpers;

namespace ECommons.GameHelpers;
public static class Map
{
    public static Vector3 PixelCoordsToWorldCoords(int x, int z, uint mapId)
    {
        var map = GetRow<Lumina.Excel.Sheets.Map>(mapId);
        var scale = (map?.SizeFactor ?? 100) * 0.01f;
        var wx = PixelCoordToWorldCoord(x, scale, map?.OffsetX ?? 0);
        var wz = PixelCoordToWorldCoord(z, scale, map?.OffsetY ?? 0);
        return new(wx, 0, wz);
    }

    // see: https://github.com/xivapi/ffxiv-datamining/blob/master/docs/MapCoordinates.md
    // see: dalamud MapLinkPayload class
    public static float PixelCoordToWorldCoord(float coord, float scale, short offset)
    {
        // +1 - networkAdjustment == 0
        // (coord / scale * 2) * (scale / 100) = coord / 50
        // * 2048 / 41 / 50 = 0.999024
        const float factor = 2048.0f / (50 * 41);
        return (coord * factor - 1024f) / scale - offset * 0.001f;
    }


    /// <summary>
    /// Finds the closest aetheryte to the given world position
    /// </summary>
    /// <returns>aetheryteId</returns>
    public static uint FindClosestAetheryte(uint territoryTypeId, Vector3 worldPos)
    {
        if(territoryTypeId == 886)
        {
            // firmament special case - just return ishgard main aetheryte
            return 70;
        }
        List<Aetheryte> aetherytes = [.. GetSheet<Aetheryte>()?.Where(a => a.Territory.RowId == territoryTypeId)];
        return aetherytes.Count > 0 ? aetherytes.MinBy(a => (worldPos - AetherytePosition(a)).LengthSquared()).RowId : 0;
    }

    /// <summary>
    /// 取得指定傳送點的世界座標。查不到該傳送點時回 <see cref="Vector3.Zero"/>。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>GetRow</c> 只是 <c>GetRowOrDefault</c> 的包裝(見
    /// <c>GenericHelpers.GetRow&lt;T&gt;</c>),回的是 <c>Aetheryte?</c>。原本的 <c>!.Value</c>
    /// 只壓抑了編譯器警告,對 <see langword="null"/> 取 <c>.Value</c> 照樣擲
    /// <see cref="System.InvalidOperationException"/> —— 等於沒判空。
    /// <paramref name="aetheryteId"/> 多半來自遊戲記憶體的即時值,可能領先本地 sheet
    /// (新版本加了傳送點)。
    /// </remarks>
    public static Vector3 AetherytePosition(uint aetheryteId)
    {
        var row = GetRow<Aetheryte>(aetheryteId);
        if(row == null)
        {
            LogMissingOnce(nameof(Aetheryte), aetheryteId,
                $"Aetheryte 表裡沒有第 {aetheryteId} 列,{nameof(AetherytePosition)} 回 Vector3.Zero");
            return Vector3.Zero;
        }
        return AetherytePosition(row.Value);
    }

    /// <summary>
    /// 取得傳送點的世界座標。連地圖標記都找不到時回 <see cref="Vector3.Zero"/>。
    /// </summary>
    /// <remarks>
    /// 🔴 原本第二段退路用的是 <c>First(...)</c>,那在找不到符合的標記時<b>擲</b>
    /// <see cref="System.InvalidOperationException"/> —— 也就是說「先 <c>FirstOrNull</c> 再退一步」
    /// 的寫法只擋掉了第一段,第二段照樣是未加防護的終結操作。
    /// <br/><br/>
    /// 🔴 <c>a.Territory.Value</c> 是 <c>RowRef</c> 解參考,指向不存在的區域時同樣擲例外。
    /// 改用 <c>ValueNullable</c> 後退成 mapId 0,而 <see cref="PixelCoordsToWorldCoords"/>
    /// 對查不到的地圖本來就有 <c>?? 100</c> / <c>?? 0</c> 的退路,不需要再多一層分支。
    /// </remarks>
    public static Vector3 AetherytePosition(Aetheryte a)
    {
        var level = a.Level[0].ValueNullable;
        if(level != null)
            return new(level.Value.X, level.Value.Y, level.Value.Z);
        var marker = GetSubrowSheet<MapMarker>()!.Flatten().FirstOrNull(m => m.DataType == 3 && m.DataKey.RowId == a.RowId)
            ?? GetSubrowSheet<MapMarker>()!.Flatten().FirstOrNull(m => m.DataType == 4 && m.DataKey.RowId == a.AethernetName.RowId);
        if(marker == null)
        {
            LogMissingOnce(nameof(MapMarker), a.RowId,
                $"MapMarker 表裡找不到傳送點 {a.RowId} 的地圖標記,{nameof(AetherytePosition)} 回 Vector3.Zero");
            return Vector3.Zero;
        }
        return PixelCoordsToWorldCoords(marker.Value.X, marker.Value.Y, a.Territory.ValueNullable?.Map.RowId ?? 0);
    }

    // if aetheryte is 'primary' (i.e. can be teleported to), return it; otherwise (i.e. aethernet shard) find and return primary aetheryte from same group
    /// <remarks>
    /// 🔴 同 <see cref="AetherytePosition(uint)"/>:<c>GetRow(...)!.Value</c> 在缺列時擲
    /// <see cref="System.InvalidOperationException"/>,<c>!</c> 只騙過編譯器不會擋下例外。
    /// <br/><br/>
    /// 📌 退路 0 不是新造的哨兵值:本方法開頭對 <c>aetheryteId == 0</c> 本來就回 0,
    /// 末尾的 <c>primary?.RowId ?? 0</c> 也是回 0,所以「查不到列」對呼叫端本來就
    /// 已經是要處理的狀態,不會多出一種。
    /// </remarks>
    public static uint FindPrimaryAetheryte(uint aetheryteId)
    {
        if(aetheryteId == 0)
            return 0;
        var rowOrNull = GetRow<Aetheryte>(aetheryteId);
        if(rowOrNull == null)
        {
            LogMissingOnce(nameof(Aetheryte), aetheryteId,
                $"Aetheryte 表裡沒有第 {aetheryteId} 列,{nameof(FindPrimaryAetheryte)} 回 0");
            return 0;
        }
        var row = rowOrNull.Value;
        if(row.IsAetheryte)
            return aetheryteId;
        var primary = GetSheet<Aetheryte>()!.FirstOrNull(a => a.AethernetGroup == row.AethernetGroup);
        return primary?.RowId ?? 0;
    }

    private static readonly HashSet<(string Sheet, uint Row)> ReportedMissingRows = [];

    /// <summary>
    /// 同一個(表,列)只記一次 —— 本類別的成員會被尋路/傳送邏輯在迴圈裡反覆呼叫,
    /// 不設閘門會把 log 灌爆。
    /// </summary>
    /// <remarks>
    /// 用 <c>Information</c> 等級是因為這是要請使用者回報的診斷,而使用者跑 LogLevel 2,
    /// <c>Debug</c>/<c>Verbose</c> 收不到。
    /// 🔴 刻意記 log 而不是靜默回預設值:缺列代表本地 sheet 與遊戲對不上,
    /// 而這裡的退路(<c>Vector3.Zero</c> / <c>0</c>)看起來都像合法值,
    /// 靜默吞掉會把看得見的錯誤變成看不見的錯誤。
    /// ⚠️ 訊息由呼叫端整句傳入(與 <c>Player.LogMissingRowOnce</c> 的樣板略有不同):
    /// MapMarker 那處查的是「對應某傳送點的標記」而不是「第 N 列」,套固定句型會說出不精確的話。
    /// </remarks>
    private static void LogMissingOnce(string sheet, uint row, string message)
    {
        lock(ReportedMissingRows)
        {
            if(!ReportedMissingRows.Add((sheet, row))) return;
        }
        PluginLog.Information($"[ECommons] {message}。(同一項只記這一次)");
    }
}
