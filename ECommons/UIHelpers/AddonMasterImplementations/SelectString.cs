using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class SelectString : AddonMasterBase<AddonSelectString>
    {
        public SelectString(nint addon) : base(addon) { }
        public SelectString(void* addon) : base(addon) { }

        public int EntryCount => Addon->PopupMenu.PopupMenu.EntryCount;
        /// <remarks>
        /// 🔴 <c>GetTextNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>&amp;node->NodeText</c>
        /// 對 null 節點<b>不會當場崩</b>:<c>NodeText</c> 在 <c>AtkTextNode</c> 偏移 0xC0,
        /// 算出的毒指標 0xC0 連 <c>ReadSeString</c> 內部的 <c>!= null</c> 都騙得過去,
        /// 直到 <c>AsSpan()</c> 去讀位址 0xC0 才炸,崩潰現場完全指不到真因。
        /// 取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString SeString
        {
            get
            {
                var node = Base->GetTextNodeById(2);
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        public string Text => SeString.GetText();

        public AtkComponentList* ListComponent => Addon->GetComponentListById(3);
        public List<Pointer<AtkComponentListItemRenderer>> ListItems
        {
            get
            {
                List<Pointer<AtkComponentListItemRenderer>> items = [];
                foreach(var node in Enumerable.Range(0, ListComponent->GetItemCount()))
                {
                    var item = ListComponent->GetItemRenderer(node);
                    if(item == null)
                        continue;
                    items.Add(item);
                }
                return items;
            }
        }

        public Entry[] Entries
        {
            get
            {
                var ret = new Entry[EntryCount];
                for(var i = 0; i < ret.Length; i++)
                {
                    ret[i] = new(this, Addon, i);
                }
                return ret;
            }
        }

        public override string AddonDescription { get; } = "Selection menu";

        public struct Entry(SelectString am, AddonSelectString* addon, int index)
        {
            private readonly AddonSelectString* Addon = addon;
            public int Index { get; init; } = index;

            public readonly AtkTextNode* TextNode => am.ListItems[Index].Value->ButtonTextNode;
            public readonly SeString SeString => MemoryHelper.ReadSeStringNullTerminated((nint)Addon->PopupMenu.PopupMenu.EntryNames[Index].Value);
            public readonly string Text => SeString.GetText();

            public readonly void Select()
            {
                Callback.Fire((AtkUnitBase*)Addon, true, Index);
            }

            public override string? ToString()
            {
                return $"AddonMaster.SelectString.Entry [Text=\"{Text}\", Index={Index}]";
            }
        }

        private void Entry1() => Entries[0].Select();
        private void Entry2() => Entries[1].Select();
        private void Entry3() => Entries[2].Select();
        private void Entry4() => Entries[3].Select();
        private void Entry5() => Entries[4].Select();
        private void Entry6() => Entries[5].Select();
        private void Entry7() => Entries[6].Select();
        private void Entry8() => Entries[7].Select();
        private void Entry9() => Entries[8].Select();
        private void Entry10() => Entries[9].Select();
        private void Entry11() => Entries[10].Select();
        private void Entry12() => Entries[11].Select();
    }
}
