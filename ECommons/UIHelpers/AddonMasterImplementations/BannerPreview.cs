using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class BannerPreview : AddonMasterBase<AtkUnitBase>
    {
        public BannerPreview(nint addon) : base(addon) { }
        public BannerPreview(void* addon) : base(addon) { }

        public AtkComponentButton* UpdateButton => Addon->GetComponentButtonById(8);
        public AtkComponentButton* CancelButton => Addon->GetComponentButtonById(9);
        /// <remarks>
        /// 🔴 <c>GetComponentNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>GetAsAtkComponent*()</c>
        /// 是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼,
        /// 當場 AccessViolation;AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到就回 null;既有消費端 <c>ClickButtonIfEnabled</c>/<c>ClickCheckboxIfEnabled</c>
        /// 開頭都已判空,所以取得到時行為完全照舊。
        /// </remarks>
        public AtkComponentCheckBox* DoNotDisplayAgainCheckbox
        {
            get
            {
                var node = Addon->GetComponentNodeById(2);
                return node == null ? null : node->GetAsAtkComponentCheckBox();
            }
        }

        public override string AddonDescription => "Portrait Update Preview";

        public void Update() => ClickButtonIfEnabled(UpdateButton);
        public void Cancel() => ClickButtonIfEnabled(CancelButton);
    }
}
