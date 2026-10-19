using ECommons.Automation;
using ECommons.UIHelpers.AtkReaderImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class RetainerList : AddonMasterBase<AddonRetainerList>
    {
        public RetainerList(nint addon) : base(addon)
        {
        }

        public RetainerList(void* addon) : base(addon)
        {
        }

        public Entry[] Retainers
        {
            get
            {
                // ReaderRetainerList.Retainers 每次存取都重讀整份清單,這裡只讀一次;每次呼叫仍回全新的陣列與 Entry。
                var list = new ReaderRetainerList(Base).Retainers;
                var entries = new Entry[list.Count];
                for(var i = 0; i < entries.Length; i++)
                {
                    entries[i] = new(Base, list[i], i);
                }
                return entries;
            }
        }

        public override string AddonDescription { get; } = "Retainer list";

        public class Entry(AtkUnitBase* Base, ReaderRetainerList.Retainer handle, int index) : ReaderRetainerList.Retainer(handle.AtkReaderParams.UnitBase, handle.AtkReaderParams.BeginOffset)
        {
            public int Index => index;
            public bool Select()
            {
                if(IsActive)
                {
                    Callback.Fire(Base, true, 2, (uint)index, Callback.ZeroAtkValue, Callback.ZeroAtkValue);
                    return true;
                }
                return false;
            }
        }

        private void Retainer1() => Retainers[0].Select();
        private void Retainer2() => Retainers[1].Select();
        private void Retainer3() => Retainers[2].Select();
        private void Retainer4() => Retainers[3].Select();
        private void Retainer5() => Retainers[4].Select();
        private void Retainer6() => Retainers[5].Select();
        private void Retainer7() => Retainers[6].Select();
        private void Retainer8() => Retainers[7].Select();
        private void Retainer9() => Retainers[8].Select();
        private void Retainer10() => Retainers[9].Select();
    }
}
