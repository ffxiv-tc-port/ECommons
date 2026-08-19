using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class PurifyResult : AddonMasterBase<AtkUnitBase>
    {
        public PurifyResult(nint addon) : base(addon) { }

        public PurifyResult(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>GetTextNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>&amp;node->NodeText</c>
        /// 對 null 節點<b>不會當場崩</b>:<c>NodeText</c> 在 <c>AtkTextNode</c> 偏移 0xC0,
        /// 算出的毒指標 0xC0 連 <c>ReadSeString</c> 內部的 <c>!= null</c> 都騙得過去,
        /// 直到 <c>AsSpan()</c> 去讀位址 0xC0 才炸,崩潰現場完全指不到真因。
        /// 取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString BannerSeString
        {
            get
            {
                var node = Base->GetTextNodeById(2);
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        public string BannerText => BannerSeString.GetText();
        public AtkComponentButton* AutomaticButton => Addon->GetComponentButtonById(19);
        public AtkComponentButton* CloseButton => Addon->GetComponentButtonById(20);

        public override string AddonDescription { get; } = "Aetherial Reduction Result";

        public void Automatic() => ClickButtonIfEnabled(AutomaticButton);
        public void Close() => ClickButtonIfEnabled(CloseButton);
    }
}
