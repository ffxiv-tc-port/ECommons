using static ECommons.LanguageHelpers.Localization;

namespace ECommons.LanguageHelpers;
#nullable disable

public static class LocalizationExtensions
{
    // 🔴 查表前一律把 key 的換行正規化成 LF。
    //
    // 為什麼:C# 11 的原始字串字面值("""...""")**逐字保留原始檔的換行序列**,不做正規化
    // (2026-08-15 對編譯出來的組件驗過:key 的 UTF-16 位元組在 DLL 裡是 CRLF 形式,LF 形式不存在)。
    // 以 CRLF 儲存的 .cs 因此會產生帶 CR 的執行期字串;而 Localization.Init 讀 ini 的第一件事
    // 就是 text.Replace("\r\n", "\n")(Localization.cs:33),表裡的 key 一律是 LF
    // ⇒ 多行 key 兩邊永遠對不起來。
    //
    // 失敗形式是**完全靜默的**:查不到就 return s,不擲例外、不寫 log,那段多行說明
    // 一直顯示英文,看起來像「還沒翻譯」而不是「翻譯壞了」。
    // 2026-08-15 實測中招:ICE 1 處、Splatoon 3 處、Questionable 3 處、TextAdvance 1 處。
    //
    // 這一行與讀檔側對稱:那邊把 ini 正規化成 LF,這邊把 key 正規化成 LF。
    // ⚠️ 只換 CRLF,不動落單的 CR —— 落單的 CR 不是換行風格差異,擅自改會動到真正的字串內容。
    internal static string NormalizeKey(string s)
        => s != null && s.Contains('\r') ? s.Replace("\r\n", "\n") : s;

    public static string Loc(this string s)
    {
        var key = NormalizeKey(s);
        if(CurrentLocalization.TryGetValue(key, out var locs) && locs != "" && locs != null)
        {
            return locs;
        }
        else if(Localization.Logging)
        {
            // 🔴 收集未命中的 key 時要寫**正規化後**的。Localization.Save 直接把這份字典
            // 寫成 ini,帶 CR 的 key 會產出一條查表端永遠對不上的條目 —— 等於用壞掉的
            // 收集結果去產生下一份壞掉的 ini。
            CurrentLocalization[key] = "";
        }
        return s;
    }

    public static string Loc(this string s, params object[] values)
    {
        var key = NormalizeKey(s);
        if(CurrentLocalization.TryGetValue(key, out var locs) && locs != "" && locs != null)
        {
            s = locs;
        }
        else if(Localization.Logging)
        {
            CurrentLocalization[key] = "";
        }
        foreach(var x in values)
        {
            s = s.ReplaceFirst(PararmeterSymbol, x.ToString());
        }
        return s;
    }
}
