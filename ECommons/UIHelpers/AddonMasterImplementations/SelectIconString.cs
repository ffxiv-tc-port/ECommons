using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using System.Collections.Generic;
using System.Linq;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class SelectIconString : AddonMasterBase<AddonSelectIconString>
    {
        public SelectIconString(nint addon) : base(addon) { }
        public SelectIconString(void* addon) : base(addon) { }

        public int EntryCount => Addon->PopupMenu.PopupMenu.EntryCount;
        public AtkComponentList* ListComponent => Addon->GetComponentListById(3);

        /// <remarks>
        /// 🔴 <see cref="ListComponent"/> 是節點取得器,找不到就<b>合法回 null</b>;
        /// 原本直接 <c>ListComponent-&gt;GetItemCount()</c> 是解空指標 = AccessViolationException,
        /// 而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。取不到時回空清單。
        /// </remarks>
        public List<Pointer<AtkComponentListItemRenderer>> ListItems
        {
            get
            {
                List<Pointer<AtkComponentListItemRenderer>> items = [];
                var list = ListComponent;
                if(list == null)
                    return items;

                foreach(var node in Enumerable.Range(0, list->GetItemCount()))
                {
                    var item = list->GetItemRenderer(node);
                    if(item == null)
                        continue;
                    items.Add(item);
                }
                return items;
            }
        }

        /// <remarks><see cref="EntryCount"/> 是原生 <c>int</c>,選單未建好時可能是負值 ⇒ 夾成空陣列。</remarks>
        public Entry[] Entries
        {
            get
            {
                var count = EntryCount;
                if(count <= 0)
                    return [];

                var ret = new Entry[count];
                for(var i = 0; i < ret.Length; i++)
                    ret[i] = new(this, Addon, i);
                return ret;
            }
        }

        public override string AddonDescription { get; } = "List selection menu with icons";

        public struct Entry(SelectIconString am, AddonSelectIconString* addon, int index)
        {
            private readonly AddonSelectIconString* Addon = addon;
            public int Index { get; init; } = index;

            /// <remarks>
            /// 🔴 <see cref="ListItems"/> 的長度與 <see cref="EntryCount"/> 是兩個獨立來源,對不上時
            /// <c>am.ListItems[Index]</c> 會擲 <see cref="System.ArgumentOutOfRangeException"/>,
            /// 而 <c>.Value</c> 本身也可能是 null(裸解參考 = 攔不到的 AccessViolationException)。
            /// 取不到時回 <see langword="null"/>。
            /// </remarks>
            public readonly AtkTextNode* TextNode
            {
                get
                {
                    var items = am.ListItems;
                    if(Index < 0 || Index >= items.Count)
                        return null;
                    var renderer = items[Index].Value;
                    return renderer == null ? null : renderer->ButtonTextNode;
                }
            }

            /// <remarks>
            /// 🔴 守衛版讀取,見 <see cref="GenericHelpers.TryGetPopupMenuEntryName"/>。
            /// 讀不到時回空字串,維持既有的非可空簽章。
            /// </remarks>
            public readonly SeString SeString
            {
                get
                {
                    if(Addon == null)
                        return string.Empty;
                    return GenericHelpers.GetPopupMenuEntryName(&Addon->PopupMenu.PopupMenu, Index) ?? (SeString)string.Empty;
                }
            }
            public readonly string Text => SeString.GetText();

            public readonly void Select()
            {
                Callback.Fire((AtkUnitBase*)Addon, true, Index);
            }

            public override string? ToString()
            {
                return $"AddonMaster.SelectIconString.Entry [Text=\"{Text}\", Index={Index}]";
            }
        }

        public void Entry1() => Entries[0].Select();
        public void Entry2() => Entries[1].Select();
        public void Entry3() => Entries[2].Select();
        public void Entry4() => Entries[3].Select();
        public void Entry5() => Entries[4].Select();
        public void Entry6() => Entries[5].Select();
        public void Entry7() => Entries[6].Select();
        public void Entry8() => Entries[7].Select();
        public void Entry9() => Entries[8].Select();
        public void Entry10() => Entries[9].Select();
        public void Entry11() => Entries[10].Select();
        public void Entry12() => Entries[11].Select();
    }
}
