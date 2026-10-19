using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
#nullable disable

namespace ECommons.UIHelpers.AtkReaderImplementations;

public unsafe class ReaderRetainerList(AtkUnitBase* Addon) : AtkReader(Addon)
{
    public uint VentureCount => ReadUInt(2) ?? 0;
    public List<Retainer> Retainers => Loop<Retainer>(3, 10, 10);

    public unsafe class Retainer(nint Addon, int start) : AtkReader(Addon, start)
    {
        // 等選人的任務每幀逐格讀名字；同樣的名字回同一個字串，不每幀重配。
        private static readonly Utf8StringCache NameCache = new(128);

        public string Name => ReadStringCached(0, NameCache);
        public uint Level => ReadUInt(2) ?? 0;
        public uint Inventory => ReadUInt(3) ?? 0;
        public uint Gil => ReadUInt(4) ?? 0;
        public bool IsActive => ReadBool(8) ?? false;
    }
}
