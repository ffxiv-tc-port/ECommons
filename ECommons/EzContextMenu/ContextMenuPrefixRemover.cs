using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ECommons.EzSharedDataManager;
using ECommons.Logging;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace ECommons.EzContextMenu;
/// <summary>
/// A helper class to remove context menu prefix that is required by Dalamud. <br></br>
/// While removing prefix, please still make sure to give an user indication that menu item is added by the plugin.
/// </summary>
public static unsafe class ContextMenuPrefixRemover
{
    private static bool Initialized = false;
    private static SeIconChar DalamudPrefix;
    private static ushort DalamudPrefixColor;

    /// <summary>
    /// Manual initialization is optional. Initialization will be automatically performed upon first call of <see cref="RemovePrefix"/> method.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            if(!Initialized)
            {
                Initialized = true;
                var data = EzSharedData.GetOrCreate<HashSet<(int, ushort)>>("ECommons.ContextMenuPrefixRemover.TakenContextMenuPrefixes", []);
                var iterations = 0;
                do
                {
                    do
                    {
                        iterations++;
                        DalamudPrefix = (SeIconChar)Random.Shared.Next(1, ushort.MaxValue);
                    }
                    while(Enum.GetValues<SeIconChar>().Contains(DalamudPrefix) && iterations < 100000);
                    DalamudPrefixColor = (ushort)Random.Shared.Next(1, ushort.MaxValue);
                }
                while(data.Contains(((int)DalamudPrefix, DalamudPrefixColor)));
                data.Add(((int)DalamudPrefix, DalamudPrefixColor));
                PluginLog.Debug($"Initialized prefix remover with prefix={(int)DalamudPrefix}, color={DalamudPrefixColor}, iterations={iterations}");
                Svc.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ContextMenu", OnContextMenuUpdate);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    internal static void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ContextMenu", OnContextMenuUpdate);
    }

    /// <summary>
    /// Call this extension method to remove prefix from menu item.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static MenuItem RemovePrefix(this MenuItem m)
    {
        Initialize();
        m.Prefix = DalamudPrefix;
        m.PrefixColor = DalamudPrefixColor;
        return m;
    }

    private static void OnContextMenuUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if(addon == null || addon->AtkValues == null || addon->AtkValuesCount <= 0) return;
        // 推不出版面時不動任何字串（前綴照樣顯示）；項目索引一律限在 AtkValuesCount 之內。
        if(!ContextMenuLayout.TryGetFirstEntryIndex(out var first)) return;
        var available = addon->AtkValuesCount - first;
        if(available <= 0) return;
        var numEntries = (int)Math.Min(addon->AtkValues[0].UInt, (uint)available);
        for(var i = 0; i < numEntries; i++)
        {
            var entry = addon->AtkValues[first + i];
            if(!entry.Type.EqualsAny(ValueType.String, ValueType.ManagedString, ValueType.String8) || entry.String.Value == null) continue;
            var original = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(entry.String.Value);
            var seString = SeString.Parse(original);
            if(seString.Payloads.Count < 2) continue;
            var removed = false;
            for(var x = 1; x < seString.Payloads.Count; x++)
            {
                if(seString.Payloads[x - 1] is UIForegroundPayload fg && fg.ColorKey == DalamudPrefixColor && seString.Payloads[x] is TextPayload payload && payload.Text != null && payload.Text.Length >= 1 && payload.Text[0] == (char)DalamudPrefix)
                {
                    seString.Payloads.RemoveAt(x);
                    seString.Payloads.Add(new TextPayload("\0"));
                    removed = true;
                    break;
                }
            }
            if(!removed) continue;
            // 原地寫回：不可超過原字串加結尾 NUL 的長度。
            var encoded = seString.Encode();
            if(encoded.Length > original.Length + 1) continue;
            Marshal.Copy(encoded, 0, (nint)entry.String.Value, encoded.Length);
        }
    }
}
