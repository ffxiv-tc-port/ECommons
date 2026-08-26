using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    /// <summary>
    /// Solo duty difficulty selection addon
    /// </summary>
    public unsafe class DifficultySelectYesNo : AddonMasterBase<AtkUnitBase>
    {
        public DifficultySelectYesNo(nint addon) : base(addon) { }

        public DifficultySelectYesNo(void* addon) : base(addon) { }

        public AtkComponentButton* ProceedButton => Addon->GetComponentButtonById(12);
        public AtkComponentButton* LeaveButton => Addon->GetComponentButtonById(13);

        /// <remarks>
        /// 🔴 <c>GetComponentNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>GetAsAtkComponent*()</c>
        /// 是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼,
        /// 當場 AccessViolation;AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到就回 null;既有消費端 <c>ClickButtonIfEnabled</c>/<c>ClickCheckboxIfEnabled</c>
        /// 開頭都已判空,所以取得到時行為完全照舊。
        /// </remarks>
        private AtkComponentRadioButton* RadioButtonById(uint id)
        {
            var node = Addon->GetComponentNodeById(id);
            return node == null ? null : node->GetAsAtkComponentRadioButton();
        }

        public AtkComponentRadioButton* NormalButton => RadioButtonById(5); // which: 64
        public AtkComponentRadioButton* EasyButton => RadioButtonById(6); // which: 65
        public AtkComponentRadioButton* VeryEasyButton => RadioButtonById(7); // which: 66

        public override string AddonDescription { get; } = "Solo duty difficulty selection window";

        public void Proceed() => ClickButtonIfEnabled(ProceedButton);
        public void Leave() => ClickButtonIfEnabled(LeaveButton);

        // TODO: needs work
        public void SetDifficultyNormal() => ClickButtonIfEnabled(NormalButton);
        public void SetDifficultyEasy() => ClickButtonIfEnabled(EasyButton);
        public void SetDifficultyVeryEasy() => ClickButtonIfEnabled(VeryEasyButton);
    }
}
