using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class MateriaAttachDialog : AddonMasterBase<AtkUnitBase>
    {
        public MateriaAttachDialog(nint addon) : base(addon)
        {
        }

        public MateriaAttachDialog(void* addon) : base(addon) { }

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public int SuccessRate => GenericHelpers.GetAtkValueInt(Base, 41);
        /// <remarks>
        /// 🔴 <c>GetTextNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>&amp;node->NodeText</c>
        /// 對 null 節點<b>不會當場崩</b>:<c>NodeText</c> 在 <c>AtkTextNode</c> 偏移 0xC0,
        /// 算出的毒指標 0xC0 連 <c>ReadSeString</c> 內部的 <c>!= null</c> 都騙得過去,
        /// 直到 <c>AsSpan()</c> 去讀位址 0xC0 才炸,崩潰現場完全指不到真因。
        /// 取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString SuccessRateSeString
        {
            get
            {
                var node = Base->GetTextNodeById(26);
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        public string SuccessRateText => SuccessRateSeString.GetText();
        public float SuccessRateFloat => SuccessRateText.Where(char.IsDigit).Append('.').Append('-').Any()
                            ? float.Parse(string.Join("", SuccessRateText.Where(char.IsDigit).Append('.').Append('-')))
                            : 0.0f;

        public AtkComponentButton* MeldButton => Base->GetComponentButtonById(35);
        public AtkComponentButton* ReturnButton => Base->GetComponentButtonById(36);

        public override string AddonDescription { get; } = "Materia melding window";

        public void Meld() => ClickButtonIfEnabled(MeldButton);
        public void Return() => ClickButtonIfEnabled(ReturnButton);
    }
}
