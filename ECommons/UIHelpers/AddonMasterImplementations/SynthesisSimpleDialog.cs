using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class SynthesisSimpleDialog : AddonMasterBase<AtkUnitBase>
    {
        public SynthesisSimpleDialog(nint addon) : base(addon) { }
        public SynthesisSimpleDialog(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>GetComponentNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>GetAsAtkComponent*()</c>
        /// 是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼,
        /// 當場 AccessViolation;AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到就回 null;既有消費端 <c>ClickButtonIfEnabled</c>/<c>ClickCheckboxIfEnabled</c>
        /// 開頭都已判空,所以取得到時行為完全照舊。
        /// </remarks>
        public AtkComponentCheckBox* UseHQMaterialsCheckbox
        {
            get
            {
                var node = Addon->GetComponentNodeById(5);
                return node == null ? null : node->GetAsAtkComponentCheckBox();
            }
        }
        public AtkComponentButton* SynthesizeButton => Addon->GetComponentButtonById(7);
        public AtkComponentButton* CancelButton => Addon->GetComponentButtonById(8);

        public override string AddonDescription => "Quick synthesis in progress window";

        public void Synthesize() => ClickButtonIfEnabled(SynthesizeButton);
        public void Cancel() => ClickButtonIfEnabled(CancelButton);
    }
}
