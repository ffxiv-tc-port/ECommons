using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class ItemInspectionResult : AddonMasterBase<AddonItemInspectionResult>
    {
        public ItemInspectionResult(nint addon) : base(addon)
        {
        }

        public ItemInspectionResult(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>GetTextNodeById</c> 找不到節點時<b>合法回 null</b>(節點編號會隨版本改,
        /// 開窗途中節點還沒建好也會回 null)。本屬性刻意保留可為 null 的語意,
        /// 呼叫端(含 <see cref="ItemName"/> 與外部消費端)必須自己判空。
        /// </remarks>
        public AtkTextNode* NameNode => Base->GetTextNodeById(26);

        /// <inheritdoc cref="NameNode"/>
        public AtkTextNode* DescNode => Base->GetTextNodeById(35);

        /// <remarks>
        /// 🔴 原本寫成 <c>ReadSeString(&amp;NameNode->NodeText)</c>,那是<b>假守衛</b>:
        /// <c>ReadSeString</c> 內部雖然有 <c>utf8String != null</c>,但 <c>NodeText</c> 位在
        /// <c>AtkTextNode</c> 的偏移 <b>0xC0</b> —— 節點為 null 時 <c>&amp;node->NodeText</c>
        /// 不會當場崩,而是靜默算出毒指標 <c>0xC0</c>,那個判空<b>照樣通過</b>,
        /// 直到 <c>AsSpan()</c> 去讀位址 0xC0 才炸,崩潰現場完全指不到真因。
        /// 取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString ItemName
        {
            get
            {
                var node = NameNode;
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }

        /// <inheritdoc cref="ItemName"/>
        public SeString Description
        {
            get
            {
                var node = DescNode;
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        public string ItemNameText => ItemName.GetText();
        public string DescriptionText => Description.GetText();

        public AtkComponentButton* NextButton => Base->GetComponentButtonById(74);
        public AtkComponentButton* CloseButton => Base->GetComponentButtonById(73);

        public override string AddonDescription { get; } = "Eureka/Bozja lootbox results";

        public void Next() => ClickButtonIfEnabled(NextButton);
        public void Close() => ClickButtonIfEnabled(CloseButton);
    }
}
