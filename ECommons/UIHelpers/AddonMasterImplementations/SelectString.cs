using Dalamud.Game.Text.SeStringHandling;
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

        public Entry[] Entries
        {
            get
            {
                var count = EntryCount;
                if(count <= 0)
                    return [];

                var ret = new Entry[count];
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

            /// <remarks>
            /// 🔴 <see cref="ListItems"/> 的長度是<b>清單元件實際有幾個 renderer</b>,與
            /// <see cref="EntryCount"/>(來自 <c>PopupMenu</c>)是兩個獨立來源,對不上時
            /// <c>am.ListItems[Index]</c> 會擲 <see cref="ArgumentOutOfRangeException"/>,
            /// 而 <c>.Value</c> 本身也可能是 null(裸解參考 = 攔不到的 AccessViolationException)。
            /// 取不到時回 <see langword="null"/> —— 呼叫端本來就在處理指標,判空是它的既有義務。
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
            /// 讀不到時回空字串,維持既有的非可空簽章(<see cref="Text"/> 與
            /// <see cref="ToString"/> 才不會 NRE)。
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
