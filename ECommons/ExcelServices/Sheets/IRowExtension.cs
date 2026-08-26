using ECommons.DalamudServices;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommons.ExcelServices.Sheets;
public interface IRowExtension<out TExtension, in TBase> : IExcelRow<TExtension> where TBase : struct, IExcelRow<TBase> where TExtension : struct, IExcelRow<TExtension>, IRowExtension<TExtension, TBase>
{
    /// <summary>
    /// 取得 <paramref name="baseRow"/> 在擴充表裡對應的列。
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>這個多載沒辦法回 null</b>:<typeparamref name="TExtension"/> 在介面上宣告成
    /// <c>out</c>(共變),把回傳型別改成 <c>TExtension?</c> 會讓它落進 <c>Nullable&lt;T&gt;</c>
    /// 這個非變異的位置,直接編不過(CS1961)。
    /// <br/>
    /// 所以這裡做的是:改用 <c>GetRowOrDefault</c> 明確判空,並在真的查不到時擲一個
    /// <b>講得出是哪張表、哪一列</b>的例外,取代 Lumina 原本那個沒有上下文的例外
    /// (基底表有這列、擴充表沒有,在台服是會發生的:兩張表的列集合不保證一致)。
    /// <br/>
    /// 📌 要「查不到就走別的路」的呼叫端請改用
    /// <c>ExcelHelpers.GetExtensionOrDefault&lt;TExtension, TBase&gt;()</c>,那個回 null。
    /// </remarks>
    static virtual TExtension GetExtended(IExcelRow<TBase> baseRow)
    {
        return Svc.Data.GetExcelSheet<TExtension>().GetRowOrDefault(baseRow.RowId)
            ?? throw new InvalidOperationException(
                $"擴充表 {typeof(TExtension).Name} 裡沒有第 {baseRow.RowId} 列(基底表 {typeof(TBase).Name} 有)。" +
                $"若這是預期內的情況,請改用 GetExtensionOrDefault。");
    }
}