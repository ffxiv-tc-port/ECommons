using System;
using System.Collections.Generic;
using System.Linq;

namespace ECommons.ImGuiMethods;
#nullable disable

/// <summary>
/// 每個列舉型別只算一次的值清單與預設標籤（<c>ToString().Replace("_", " ")</c>），給每幀畫的列舉選單用。
/// 靜態泛型類別：型別初始化由 CLR 保證只跑一次且執行緒安全。回傳的陣列是共用的，呼叫端不可修改。
/// </summary>
internal static class EnumCache<T> where T : Enum
{
    private static readonly T[] ValuesOrNull;
    private static readonly string[] LabelsOrNull;
    private static readonly Dictionary<T, string> LabelByValue;

    static EnumCache()
    {
        // GetValues 對這個 T 擲例外時（例如 T 就是 System.Enum）留 null，讓呼叫端照舊在原處擲同一個例外。
        try
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var labels = new string[values.Length];
            var byValue = new Dictionary<T, string>(values.Length);
            for(var i = 0; i < values.Length; i++)
            {
                labels[i] = values[i].ToString().Replace("_", " ");
                byValue.TryAdd(values[i], labels[i]);
            }
            ValuesOrNull = values;
            LabelsOrNull = labels;
            LabelByValue = byValue;
        }
        catch
        {
            ValuesOrNull = null;
            LabelsOrNull = null;
            LabelByValue = null;
        }
    }

    internal static T[] Values => ValuesOrNull ?? Enum.GetValues(typeof(T)).Cast<T>().ToArray();

    /// <summary>與 <see cref="Values"/> 同索引的預設標籤。</summary>
    internal static string[] Labels => LabelsOrNull ?? Values.Select(x => x.ToString().Replace("_", " ")).ToArray();

    /// <summary>等同 <c>value.ToString().Replace("_", " ")</c>；不在清單裡的值（未命名、旗標組合）即時算。</summary>
    internal static string Label(T value)
    {
        if(LabelByValue != null && LabelByValue.TryGetValue(value, out var label)) return label;
        return value.ToString().Replace("_", " ");
    }
}
