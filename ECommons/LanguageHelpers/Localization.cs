using Dalamud;
using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ECommons.LanguageHelpers;
#nullable disable

public static class Localization
{
    public static string PararmeterSymbol = "??";
    public static string Separator = "==";
    internal static Dictionary<string, string> CurrentLocalization = [];
    internal static List<string> AvailableLanguages;
    public static string CurrentLanguage { get; internal set; } = null;
    public static bool Logging = false;

    public static void Init(string Language = null)
    {
        CurrentLocalization.Clear();
        CurrentLanguage = null;
        if(Language != null)
        {
            var file = GetLocFileLocation(Language);
            if(File.Exists(file))
            {
                CurrentLanguage = Language;
                var text = File.ReadAllText(file, Encoding.UTF8);
                var list = text.Replace("\r\n", "\n").Replace("\r", "").Split("\n");
                for(var i = 0; i < list.Length; i++)
                {
                    var x = list[i].Replace("\\n", "\n");
                    // 🔴 空行/純空白行直接跳過，不算「無效條目」。
                    // Split 對「檔尾有換行」的檔必定多產生一個空字串元素，
                    // 原寫法因此讓每個帶 Language*.ini 的外掛開場固定印一則 Invalid entry，
                    // 行號恆等於檔案行數 —— 那是全艦隊常態雜訊，曾把崩潰鑑識帶往錯的方向。
                    if(string.IsNullOrWhiteSpace(x)) continue;
                    var e = x.Split(Separator);
                    if(e.Length == 2)
                    {
                        if(CurrentLocalization.ContainsKey(e[0]))
                        {
                            PluginLog.Warning($"[Localization] Duplicate localization entry {e[0]} found in localization file {file}");
                        }
                        CurrentLocalization[e[0]] = e[1];
                    }
                    else
                    {
                        PluginLog.Warning($"[Localization] Invalid entry {x} (line {i + 1}) found in localization file {file}");
                    }
                }
                PluginLog.Information($"[Localization] Loaded {CurrentLocalization.Count} entries");
            }
            else
            {
                PluginLog.Information($"[Localization] Requested localization file {file} does not exists");
            }
        }
        else
        {
            PluginLog.Information("[Localization] No special localization");
        }
    }

    public static List<string> GetAvaliableLanguages(bool rescan = false)
    {
        if(AvailableLanguages == null || rescan)
        {
            AvailableLanguages = ["English"];
            foreach(var x in Directory.GetFiles(Svc.PluginInterface.AssemblyLocation.DirectoryName))
            {
                var name = Path.GetFileName(x);
                if(name.StartsWith("Language") && name.EndsWith(".ini"))
                {
                    var lang = name[8..^4];
                    if(!AvailableLanguages.Contains(lang)) AvailableLanguages.Add(lang);
                    PluginLog.Information($"[Localization] Found language data {lang}");
                }
            }
        }
        return AvailableLanguages;
    }

    // 這裡刻意用數字轉型而不是列舉成員名稱：上游 Dalamud 的 ClientLanguage 只有 0..3，
    // 4 以上是各分支自己加的，寫成員名稱在對上游建置時會編不過。
    // 我們這一支（TC fork）的 ClientLanguage 是：
    //   0 Japanese / 1 English / 2 German / 3 French
    //   4 ChineseSimplified / 5 ChineseTraditional / 6 Korean / 7 TraditionalChinese
    // 🔴 台服（TC）實機回報的是 7，不是 4。原本只對應 4 的時候，台服會靜默落到 "English"，
    //    連帶讓 Language*.ini 的查找也找錯檔名。4 保留不動（別的分支還在用）。
    public static string GameLanguageString => Svc.Data.Language switch
    {
        ClientLanguage.Japanese => "Japanese",
        ClientLanguage.French => "French",
        ClientLanguage.German => "German",
        (ClientLanguage)4 => "Chinese",
        (ClientLanguage)5 => "ChineseTraditional",
        (ClientLanguage)6 => "Korean",
        (ClientLanguage)7 => "ChineseTraditional",
        _ => "English"
    };

    public static void Save(string lang)
    {
        var file = GetLocFileLocation(lang);
        File.WriteAllText(file, CurrentLocalization.Select(x => $"{x.Key.Replace("\n", "\\n")}{Separator}{x.Value.Replace("\n", "\\n")}").Join("\n"));
    }

    public static string GetLocFileLocation(string lang)
    {
        return Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, $"Language{lang}.ini");
    }

    public static string Loc(string s)
    {
        return s.Loc();
    }

    public static string Loc(string s, params object[] values)
    {
        return s.Loc(values);
    }
}
