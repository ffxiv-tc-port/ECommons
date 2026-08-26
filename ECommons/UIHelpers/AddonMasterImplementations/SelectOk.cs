using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class SelectOk : AddonMasterBase<AddonSelectOk>
    {
        public SelectOk(nint addon) : base(addon)
        {
        }

        public SelectOk(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>PromptText</c> 是偏移 0x238 的<b>指標欄位</b>,開窗途中/版面未建好時為 null。
        /// 原本的 <c>ReadSeString(&amp;Addon->PromptText->NodeText)</c> 是<b>假守衛</b>:
        /// <c>ReadSeString</c> 內部雖有 <c>utf8String != null</c>,但 <c>NodeText</c> 在
        /// <c>AtkTextNode</c> 偏移 0xC0 —— 節點為 null 時 <c>&amp;node->NodeText</c> 不會當場崩,
        /// 而是靜默算出毒指標 0xC0,那個判空<b>照樣通過</b>,直到 <c>AsSpan()</c> 讀位址 0xC0 才炸,
        /// 崩潰現場完全指不到真因。取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString SeString
        {
            get
            {
                var node = Addon->PromptText;
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        public string Text => SeString.GetText();

        public override string AddonDescription { get; } = "Generic confirmation window (OK button)";

        public void Ok() => ClickButtonIfEnabled(Addon->OkButton);
    }
}
