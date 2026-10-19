using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;

namespace ECommons.UIHelpers.AtkReaderImplementations;
#nullable disable

public unsafe class ReaderContextMenu(AtkUnitBase* Addon) : AtkReader(Addon)
{
    public uint Count => ReadUInt(0) ?? 0;
    /// <remarks>推不出項目從 AtkValues 哪一格開始時回空清單，見 <see cref="ContextMenuLayout"/>。</remarks>
    public List<ContextMenuEntry> Entries => ContextMenuLayout.TryGetFirstEntryIndex(out var first) ? Loop<ContextMenuEntry>(first, 1, (int)Count) : [];

    public unsafe class ContextMenuEntry(nint Addon, int start) : AtkReader(Addon, start)
    {
        public string Name => ReadString(0);
        public SeString NameSeString => ReadSeString(0);
    }
}
