using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Text.ReadOnly;
using System.Collections.Generic;
using System.Linq;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class ContextMenu : AddonMasterBase<AddonContextMenu>
    {
        public ContextMenu(nint addon) : base(addon) { }
        public ContextMenu(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>EntriesCount</c> 的來源 <c>AtkValues[0]</c> 本身也要驗:選單剛開、或這個 addon
        /// 還沒填完值時 <c>AtkValuesCount</c> 可能是 0,原寫法是無界讀,讀到的數字會被
        /// <see cref="Entries"/> 拿去配置陣列、也會被當成迴圈上限。出界時回 0(＝沒有項目)。
        /// </remarks>
        public int EntriesCount => (int)GenericHelpers.GetAtkValueUInt(Base, 0);

        public AtkComponentList* ListComponent => Addon->GetComponentListById(2);

        /// <remarks>
        /// 🔴 <see cref="ListComponent"/> 是節點取得器,找不到就<b>合法回 null</b>;
        /// 原本直接 <c>ListComponent-&gt;GetItemCount()</c> 是解空指標 = AccessViolationException,
        /// 而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到清單元件時回空清單。
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

        private const int offset = 7;
        public Entry[] Entries
        {
            get
            {
                var count = EntriesCount;
                if(count <= 0)
                    return [];

                var ret = new Entry[count];
                for(var i = 0; i < ret.Length; i++)
                    ret[i] = new(this, Addon, i);
                return ret;
            }
        }

        public override string AddonDescription { get; } = "Context menu";

        public readonly struct Entry(ContextMenu am, AddonContextMenu* addon, int index)
        {
            private readonly AddonContextMenu* Addon = addon;
            public readonly int Index { get; init; } = index;
            public readonly int ListIndex => Index + offset;
            // Dalamud added context menu entries all have a callback index of -1, which results in looping the list and calling something else. AFAIK, native entries are always a single payload of rawtext.
            /// <remarks>
            /// 🔴 原寫法在 <c>Type</c> 檢查<b>之後</b>才取 <c>String.Value</c>,但少了兩道:
            /// ①<c>ListIndex</c> 沒有對 <c>AtkValuesCount</c> 做邊界檢查(選單項目少於 <c>ListIndex</c>
            /// 時是無界讀);②型別是 <c>ManagedString</c> 但指標為 null 時,
            /// <c>ReadOnlySeStringSpan</c> 會從位址 0 起算 —— 兩者都是 AccessViolationException,
            /// 而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
            /// </remarks>
            public readonly bool IsNativeEntry
            {
                get
                {
                    if(!GenericHelpers.TryGetAtkValue((AtkUnitBase*)Addon, ListIndex, out var value))
                        return false;
                    if(value.Type != FFXIVClientStructs.FFXIV.Component.GUI.ValueType.ManagedString || value.String.Value == null)
                        return false;
                    return new ReadOnlySeStringSpan(value.String.Value).PayloadCount == 1;
                }
            }

            /// <remarks>
            /// 🔴 <see cref="ListItems"/> 的長度是<b>清單元件實際有幾個 renderer</b>,與
            /// <see cref="EntriesCount"/>(來自 AtkValues)是兩個獨立來源,兩者不一致時
            /// <c>am.ListItems[Index]</c> 會擲 <see cref="System.ArgumentOutOfRangeException"/>;
            /// 而 <c>.Value</c> 也可能是 null。取不到時回 <see langword="null"/> ——
            /// 呼叫端本來就在處理指標,判空是它的既有義務。
            /// </remarks>
            public AtkTextNode* TextNode
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
            /// 🔴 三重守衛(索引在界內／型別真的是字串／字串指標非空),
            /// 見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>。
            /// 讀不到時回空字串,維持既有的非可空簽章(<see cref="Text"/> 與
            /// <see cref="ToString"/> 才不會 NRE)。
            /// </remarks>
            public readonly SeString SeString => GenericHelpers.GetAtkValueSeString((AtkUnitBase*)Addon, ListIndex) ?? (SeString)string.Empty;
            public readonly string Text => SeString.GetText();

            /// <remarks>索引與 <see cref="ListItems"/> 對不上時回 <see langword="false"/>,理由見 <see cref="TextNode"/>。</remarks>
            public readonly bool Enabled
            {
                get
                {
                    var items = am.ListItems;
                    return Index >= 0 && Index < items.Count && GenericHelpers.IsComponentEnabled(items[Index].Value);
                }
            }

            public readonly bool Select()
            {
                if(IsNativeEntry && Enabled)
                {
                    Callback.Fire((AtkUnitBase*)Addon, true, 0, Index, 0);
                    return true;
                }
                return false;
            }

            public override readonly string? ToString() => $"{nameof(AddonMaster)}.{nameof(ContextMenu)}.{nameof(Entry)} [Text=\"{Text}\", Index={ListIndex} CallbackIndex={Index}]";
        }
    }
}
