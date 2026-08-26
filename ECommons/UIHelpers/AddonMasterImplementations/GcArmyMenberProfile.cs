using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class GcArmyMenberProfile : AddonMasterBase<AtkUnitBase>
    {
        public GcArmyMenberProfile(nint addon) : base(addon) { }
        public GcArmyMenberProfile(void* addon) : base(addon) { }

        public AtkComponentButton* ViewMembersButton => Addon->GetComponentButtonById(2);
        public AtkComponentButton* CloseButton => Addon->GetComponentButtonById(3);

        public void ViewMembers() => ClickButtonIfEnabled(ViewMembersButton);
        public void Close() => ClickButtonIfEnabled(CloseButton);

        public AtkComponentButton* QuestionButton => Addon->GetComponentButtonById(36);
        public AtkComponentButton* PostponeButton => Addon->GetComponentButtonById(37);
        public AtkComponentButton* DismissButton => Addon->GetComponentButtonById(38);

        public void Question() => ClickButtonIfEnabled(QuestionButton);
        public void Postpone() => ClickButtonIfEnabled(PostponeButton);
        public void Dismiss() => ClickButtonIfEnabled(DismissButton);

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

        public AtkComponentRadioButton* DisplayOrdersButton => RadioButtonById(31);
        public AtkComponentRadioButton* ChangeClassButton => RadioButtonById(32);
        public AtkComponentRadioButton* ConfirmChemistryButton => RadioButtonById(33);
        public AtkComponentRadioButton* OutfitButton => RadioButtonById(34);

        public override string AddonDescription { get; } = "Squadron member profile window";

        public void DisplayOrders() => ClickButtonIfEnabled(DisplayOrdersButton);
        public void ChangeClass() => ClickButtonIfEnabled(ChangeClassButton);
        public void ConfirmChemistry() => ClickButtonIfEnabled(ConfirmChemistryButton);
        public void Outfit() => ClickButtonIfEnabled(OutfitButton);
    }
}
