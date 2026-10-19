using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ECommons.ImGuiMethods;
public static unsafe partial class ImGuiEx
{
    /// <summary>
    /// Checkbox that adds/removes a set of values from the collection. Displays check mark when element is present in a collection, and doesn't displays checkmark when element is not present.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="label"></param>
    /// <param name="values">A set of values to add/remove</param>
    /// <param name="collection">A collection that will be modified</param>
    /// <param name="inverted">Whether to invert checkbox. Not implemented.</param>
    /// <param name="delayedOperation">When set to true, will schedule the change in next framework update. Useful when you want to modify a collection while iterating over it.</param>
    /// <returns></returns>
    public static bool CollectionCheckbox<T>(string label, IEnumerable<T> values, ICollection<T> collection, bool inverted = false, bool delayedOperation = false)
    {
        if(!values.Any())
        {
            var x = false;
            ImGui.BeginDisabled();
            ImGui.Checkbox(label, ref x);
            ImGui.EndDisabled();
            return false;
        }
        if(!inverted)
        {
            if(collection.ContainsAll(values))
            {
                var x = true;
                if(ImGui.Checkbox(label, ref x))
                {
                    Execute(CollectionRemoveAll(values, collection), delayedOperation);
                    return true;
                }
            }
            else
            {
                var x = collection.ContainsAny(values);
                if(ImGuiEx.CheckboxBullet(label, ref x))
                {
                    Execute(CollectionAddAll(values, collection), delayedOperation);
                    return true;
                }
            }
        }
        else
        {
            if(!collection.ContainsAny(values))
            {
                var x = true;
                if(ImGui.Checkbox(label, ref x))
                {
                    Execute(CollectionAddAll(values, collection), delayedOperation);
                    return true;
                }
            }
            else
            {
                var x = !collection.ContainsAll(values);
                if(ImGuiEx.CheckboxBullet(label, ref x))
                {
                    Execute(CollectionRemoveAll(values, collection), delayedOperation);
                    return true;
                }
            }
        }
        return false;
    }

    // 閉包只在真的點下去時才建立：這些勾選框每幀每列都畫，沒點的那幾幀不該配置。
    private static Action CollectionRemoveAll<T>(IEnumerable<T> values, ICollection<T> collection) => () =>
    {
        foreach(var el in values)
        {
            collection.Remove(el);
        }
    };

    private static Action CollectionAddAll<T>(IEnumerable<T> values, ICollection<T> collection) => () =>
    {
        foreach(var el in values)
        {
            collection.Remove(el);
            collection.Add(el);
        }
    };

    /// <summary>
    /// Checkbox that adds/removes a value from the collection. Displays check mark when element is present in a collection, and doesn't displays checkmark when element is not present.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="label"></param>
    /// <param name="value">A value to add/remove</param>
    /// <param name="collection">A collection that will be modified</param>
    /// <param name="inverted">Whether to invert checkbox.</param>
    /// <param name="delayedOperation">When set to true, will schedule the change in next framework update. Useful when you want to modify a collection while iterating over it.</param>
    /// <returns></returns>
    public static bool CollectionCheckbox<T>(string label, T value, ICollection<T> collection, bool inverted = false, bool delayedOperation = false)
    {
        // 與 CollectionCore(x => ImGui.Checkbox(label, ref x), …) 相同，只是不為每次呼叫建立 draw 委派。
        var x = collection.Contains(value);
        if(inverted) x = !x;
        if(ImGui.Checkbox(label, ref x))
        {
            Execute(CollectionApply(x, value, collection, inverted), delayedOperation);
            return true;
        }
        return false;
    }

    public delegate bool CollectionCoreDelegate(ref bool contains);
    public static bool CollectionCore<T>(CollectionCoreDelegate draw, T value, ICollection<T> collection, bool inverted = false, bool delayedOperation = false)
    {
        var x = collection.Contains(value);
        if(inverted) x = !x;
        if(draw(ref x))
        {
            Execute(CollectionApply(x, value, collection, inverted), delayedOperation);
            return true;
        }
        return false;
    }

    private static Action CollectionApply<T>(bool x, T value, ICollection<T> collection, bool inverted) => delegate
    {
        if(inverted) x = !x;
        if(x)
        {
            collection.Add(value);
        }
        else
        {
            while(collection.Contains(value))
            {
                if(!collection.Remove(value)) break;
            }
        }
    };

    public static bool CollectionButtonCheckbox<T>(FontAwesomeIcon icon, T value, ICollection<T> collection, bool smallButton = false, bool inverted = false)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var ret = CollectionButtonCheckbox(icon.ToIconString(), value, collection, EColor.Red, smallButton, inverted);
        ImGui.PopFont();
        return ret;
    }

    public static bool CollectionButtonCheckbox<T>(string name, T value, ICollection<T> collection, bool smallButton = false, bool inverted = false) => CollectionButtonCheckbox(name, value, collection, EColor.Red, smallButton, inverted);

    public static bool CollectionButtonCheckbox<T>(string name, T value, ICollection<T> collection, Vector4 color, bool smallButton = false, bool inverted = false)
    {
        var col = collection.Contains(value);
        if(inverted) col = !col;
        var ret = false;
        if(col)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
        }
        if(smallButton ? ImGui.SmallButton(name) : ImGui.Button(name))
        {
            if(col)
            {
                if(inverted)
                {
                    collection.Add(value);
                }
                else
                {
                    collection.Remove(value);
                }
            }
            else
            {
                if(inverted)
                {
                    collection.Remove(value);
                }
                else
                {
                    collection.Add(value);
                }
            }
            ret = true;
        }
        if(col) ImGui.PopStyleColor(3);
        return ret;
    }
}
