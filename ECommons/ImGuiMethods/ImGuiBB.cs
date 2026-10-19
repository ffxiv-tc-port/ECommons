using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using static ECommons.GenericHelpers;

namespace ECommons.ImGuiMethods;

public static class ImGuiBB
{
    private static Dictionary<int, List<Action>> Cache = [];

    public static void Text(string str, bool ignoreCache = false)
    {
        Safe(delegate
        {
            var result = Regex.Split(str, @"(\[color=[0-9a-z#]+\])|(\[\/color\])|(\[size=[0-9\.\,]+\])|(\[\/size\])|(\[img\].*?\[\/img\])|(\n)", RegexOptions.IgnoreCase);
            var first = false;
            // 不變式:顏色推幾個就彈幾個、字級開過就還原。多出來的結束標記忽略;沒關的(含中途擲例外)在這次繪製結束前補上。
            // ImGui 的 PopStyleColor 沒有下溢防護,多彈一次會靜默寫壞樣式表。
            var colorsPushed = 0;
            var sizesOpen = 0;
            try
            {
                foreach(var s in result)
                {
                    //PluginLog.Information($"{s.Replace("\n", @"\n")}");
                    if(s == "\n")
                    {
                        first = false;
                        continue;
                    }
                    if(s == String.Empty) continue;
                    if(s.StartsWith("[color=", StringComparison.OrdinalIgnoreCase))
                    {
                        var col = s[7..^1].Replace("#", "");
                        int r = 0, g = 0, b = 0;
                        if(col.Length == 3)
                        {
                            int.TryParse(col[0..1] + col[0..1], System.Globalization.NumberStyles.HexNumber, null, out r);
                            int.TryParse(col[1..2] + col[1..2], System.Globalization.NumberStyles.HexNumber, null, out g);
                            int.TryParse(col[2..3] + col[2..3], System.Globalization.NumberStyles.HexNumber, null, out b);
                        }
                        else if(col.Length == 6)
                        {
                            int.TryParse(col[0..2], System.Globalization.NumberStyles.HexNumber, null, out r);
                            int.TryParse(col[2..4], System.Globalization.NumberStyles.HexNumber, null, out g);
                            int.TryParse(col[4..6], System.Globalization.NumberStyles.HexNumber, null, out b);
                        }
                        var col4 = new Vector4((float)r / 255f, (float)g / 255f, (float)b / 255f, 1f);
                        ImGui.PushStyleColor(ImGuiCol.Text, col4);
                        colorsPushed++;
                    }
                    else if(s.Equals("[/color]", StringComparison.OrdinalIgnoreCase))
                    {
                        if(colorsPushed > 0)
                        {
                            ImGui.PopStyleColor();
                            colorsPushed--;
                        }
                    }
                    else if(s.StartsWith("[size=", StringComparison.OrdinalIgnoreCase))
                    {
                        sizesOpen++;
                        if(float.TryParse(s[6..^1], out var size) && size >= 0.1 && size <= 50)
                        {
                            ImGui.SetWindowFontScale(size);
                        }
                    }
                    else if(s.Equals("[/size]", StringComparison.OrdinalIgnoreCase))
                    {
                        if(sizesOpen > 0)
                        {
                            sizesOpen--;
                            ImGui.SetWindowFontScale(1f);
                        }
                    }
                    else if(s.StartsWith("[img]", StringComparison.OrdinalIgnoreCase))
                    {

                        var imageUrl = s[5..^6];
                        if(ThreadLoadImageHandler.TryGetTextureWrap(imageUrl, out var texture))
                        {
                            if(first)
                            {
                                ImGui.SameLine(0, 0);
                            }
                            else
                            {
                                first = true;
                            }
                            ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                        }
                    }
                    else
                    {
                        if(first)
                        {
                            ImGui.SameLine(0, 0);
                        }
                        else
                        {
                            first = true;
                        }
                        ImGui.Text(s);
                    }
                }
            }
            finally
            {
                if(colorsPushed > 0) ImGui.PopStyleColor(colorsPushed);
                if(sizesOpen > 0) ImGui.SetWindowFontScale(1f);
            }
        });
    }
}
