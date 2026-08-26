using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Buffers.Binary;
using System.Numerics;

namespace ECommons.ImGuiMethods;

/// <summary>
/// Unified colour wrapper. Implicitly converts to needed formats and supports importing from most formats.
/// </summary>
/// <remarks>
/// <para>Contains built in redefinable colors (refinable after calling ECommonsMain.Init)</para>
/// </remarks>
public struct EzColor
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
    public readonly Vector4 Vector4 => (Vector4)this;
    [Obsolete("Use ARGB")]
    public readonly uint U32 => ImGui.ColorConvertFloat4ToU32(this);
    public readonly uint ARGB => ((uint)(A * 255) << 24) | ((uint)(R * 255) << 16) | ((uint)(G * 255) << 8) | (uint)(B * 255);
    public readonly uint RGBA => ((uint)(R * 255) << 24) | ((uint)(G * 255) << 16) | ((uint)(B * 255) << 8) | (uint)(A * 255);

    public EzColor() { }
    public EzColor(Vector4 vec) : this(vec.X, vec.Y, vec.Z, vec.W) { }
    /// <summary>
    /// Takes in uints as 0xRRGGBB or 0xRRGGBBAA
    /// </summary>
    public EzColor(uint col)
    {
        var withAlpha = AppendAlpha(col);
        R = ((withAlpha >> 24) & 0xFF) / 255f;
        G = ((withAlpha >> 16) & 0xFF) / 255f;
        B = ((withAlpha >> 8) & 0xFF) / 255f;
        A = (withAlpha & 0xFF) / 255f;
    }

    public EzColor(float r, float g, float b, float a = 1)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public override readonly string ToString() => $"RGBA: [{R}, {G}, {B}, {A}] {nameof(Vector4)}: [{Vector4}] {nameof(ARGB)}: [{ARGB:X8}] {nameof(RGBA)}: [{RGBA:X8}]";

    public static EzColor From(float r, float g, float b, float a = 1)
        => new() { R = r, G = g, B = b, A = a };

    public static EzColor From(Vector3 col, float alpha = 1)
        => From(col.X, col.Y, col.Z, alpha);

    public static EzColor From(Vector4 col)
        => From(col.X, col.Y, col.Z, col.W);

    /// <summary>
    /// Takes in uints as 0xRRGGBB or 0xRRGGBBAA
    /// </summary>
    public static EzColor From(uint col)
        => new(col);

    public static EzColor From(ImGuiCol col)
        => From(ImGui.GetColorU32(col));

    public static EzColor FromARGB(uint argb)
        => From((argb >> 24) | (argb << 8));

    public static EzColor FromABGR(uint abgr)
        => From(BinaryPrimitives.ReverseEndianness(abgr));

    /// <summary>
    /// 資料表裡查不到指定 id 時的退路顏色:不透明白。
    /// </summary>
    /// <remarks>
    /// 🔴 <b>不能</b>用 <c>default</c>/透明黑當退路 —— 那會讓文字或圖形<b>直接消失</b>,
    /// 使用者看到的是「什麼都沒畫」而不是「顏色查不到」,是最難回報的失敗形式。
    /// 白色至少保證東西還看得見,而且明顯不同於任何 UIColor 配色,一眼就知道是退路值。
    /// </remarks>
    private static EzColor UnknownColorFallback => new(0xFFFFFFu);

    /// <summary>
    /// 由 <c>UIColor</c> 表取前景色。查不到 <paramref name="id"/> 時回
    /// <see cref="UnknownColorFallback"/>(不透明白),不擲例外。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>GetRow</c> 對不存在的列<b>擲例外</b>不回 null,而這幾個函式吃的是呼叫端傳進來的動態 id,
    /// 且幾乎都在 ImGui 的繪製迴圈裡被呼叫 —— 一擲例外整個視窗就不畫了。
    /// ⚠️ 台服的 <c>UIColor</c>/<c>Stain</c> 表與國際服不保證同步,寫死的 id 在這裡是靜默的錯誤來源。
    /// </remarks>
    public static EzColor FromUiForeground(uint id)
    {
        var row = Svc.Data.GetExcelSheet<UIColor>().GetRowOrDefault(id);
        return row == null ? UnknownColorFallback : FromABGR(row.Value.Dark);
    }

    /// <inheritdoc cref="FromUiForeground"/>
    public static EzColor FromUiGlow(uint id)
    {
        var row = Svc.Data.GetExcelSheet<UIColor>().GetRowOrDefault(id);
        return row == null ? UnknownColorFallback : FromABGR(row.Value.Light);
    }

    /// <summary>
    /// 由 <c>Stain</c>(染劑)表取顏色。查不到 <paramref name="id"/> 時回
    /// <see cref="UnknownColorFallback"/>(不透明白),不擲例外。理由同 <see cref="FromUiForeground"/>。
    /// </summary>
    public static EzColor FromStain(uint id)
    {
        var row = Svc.Data.GetExcelSheet<Stain>().GetRowOrDefault(id);
        return row == null
            ? UnknownColorFallback
            : From(BinaryPrimitives.ReverseEndianness(row.Value.Color) >> 8) with { A = 1 };
    }

    public static implicit operator Vector4(EzColor col)
        => new(col.R, col.G, col.B, col.A);

    public static implicit operator uint(EzColor col)
        => ImGui.ColorConvertFloat4ToU32(col);

    private static uint AppendAlpha(uint col) => (col & 0xFFFFFF) == col ? (col << 8) | 0xFF : col;

    public static EzColor RedBright { get; set; } = new(0xFF0000);
    public static EzColor Red { get; set; } = new(0xAA0000);
    public static EzColor RedDark { get; set; } = new(0x440000);
    public static EzColor GreenBright { get; set; } = new(0x00FF00);
    public static EzColor Green { get; set; } = new(0x00AA00);
    public static EzColor GreenDark { get; set; } = new(0x004400);
    public static EzColor BlueBright { get; set; } = new(0x0000FF);
    public static EzColor Blue { get; set; } = new(0x0000AA);
    public static EzColor BlueSea { get; set; } = new(0x0058AA);
    public static EzColor BlueSky { get; set; } = new(0x0085FF);
    public static EzColor White { get; set; } = new(0xFFFFFF);
    public static EzColor Black { get; set; } = new(0x000000);
    public static EzColor Transparent { get; set; } = new(0x00000000);
    public static EzColor YellowBright { get; set; } = new(0xFFFF00);
    public static EzColor Yellow { get; set; } = new(0xAAAA00);
    public static EzColor YellowDark { get; set; } = new(0x444400);
    public static EzColor OrangeBright { get; set; } = new(0xFF7F00);
    public static EzColor Orange { get; set; } = new(0xAA5400);
    public static EzColor CyanBright { get; set; } = new(0x00FFFF);
    public static EzColor Cyan { get; set; } = new(0x00AAAA);
    public static EzColor VioletBright { get; set; } = new(0xFF00FF);
    public static EzColor Violet { get; set; } = new(0xAA00AA);
    public static EzColor VioletDark { get; set; } = new(0x440044);
    public static EzColor PurpleBright { get; set; } = new(0xFF0084);
    public static EzColor Purple { get; set; } = new(0xAA0058);
    public static EzColor PinkLight { get; set; } = new(0xFFABD6);
}