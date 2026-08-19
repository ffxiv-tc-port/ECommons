using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using System.Collections.Generic;
using System.Linq;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class ItemSearchResult : AddonMasterBase<AddonItemSearchResult>
    {
        public ItemSearchResult(nint addon) : base(addon) { }
        public ItemSearchResult(void* addon) : base(addon) { }

        public AtkComponentList* ListComponent => Addon->Results;
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
                var ret = new Entry[ListItems.Count];
                for(var i = 0; i < ret.Length; i++)
                    ret[i] = new(this, Addon, i);
                return ret;
            }
        }

        public override string AddonDescription { get; } = "Marketboard Item Listings";

        public readonly struct Entry(ItemSearchResult am, AddonItemSearchResult* addon, int index)
        {
            private readonly AddonItemSearchResult* Addon = addon;
            public readonly int Index { get; init; } = index;

            /// <remarks>
            /// 🔴 這六個屬性原本各自把一條五跳鏈全裸展開,每一跳都是獨立的 null 路徑:
            /// <c>ListItems</c> 每次存取都重新建清單(<c>Index</c> 可能已越界⇒<c>ArgumentOutOfRangeException</c>)、
            /// <c>.Value</c>、<c>->ComponentNode</c>、<c>->Component</c> 皆可為 null,
            /// 末端的 <c>GetAs*Node()</c> 更是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c>
            /// 交給遊戲原生碼當場 AccessViolation,而 AVE 是 corrupted-state exception,try/catch 攔不到。
            /// 本助手把前四跳收斂成一次判空,取不到回 null(六個屬性隨之回 null,保持指標語意)。
            /// </remarks>
            private AtkComponentBase* ListItemComponent
            {
                get
                {
                    var items = am.ListItems;
                    if(Index < 0 || Index >= items.Count) return null;
                    var renderer = items[Index].Value;
                    if(renderer == null) return null;
                    var componentNode = renderer->ComponentNode;
                    return componentNode == null ? null : componentNode->Component;
                }
            }

            public AtkImageNode* HQImageNode
            {
                get
                {
                    var component = ListItemComponent;
                    if(component == null) return null;
                    var node = component->GetImageNodeById(3);
                    return node == null ? null : node->GetAsAtkImageNode();
                }
            }

            public AtkTextNode* MateriaTextNode => TextNodeById(4);
            public AtkTextNode* PriceTextNode => TextNodeById(5);
            public AtkTextNode* QuantityTextNode => TextNodeById(6);
            public AtkTextNode* TotalTextNode => TextNodeById(8);
            public AtkTextNode* RetainerTextNode => TextNodeById(10);

            /// <inheritdoc cref="ListItemComponent"/>
            private AtkTextNode* TextNodeById(uint id)
            {
                var component = ListItemComponent;
                if(component == null) return null;
                var node = component->GetTextNodeById(id);
                return node == null ? null : node->GetAsAtkTextNode();
            }

            // TODO: Select function
            // Notes: A callback is sufficient if ItemSearchResult is from the marketboard. If it's your own listing, it requires a synthesised ListItemToggle event
        }
    }
}
