using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class MiragePrismPrismSetConvertC : AddonMasterBase<AtkUnitBase>
    {
        public MiragePrismPrismSetConvertC(nint addon) : base(addon) { }
        public MiragePrismPrismSetConvertC(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>GetComponentNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>GetAsAtkComponent*()</c>
        /// 是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼,
        /// 當場 AccessViolation;AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到就回 null;既有消費端 <c>ClickButtonIfEnabled</c>/<c>ClickCheckboxIfEnabled</c>
        /// 開頭都已判空,所以取得到時行為完全照舊。
        /// </remarks>
        public AtkComponentCheckBox* StoreAsOutfitGlamourCheckBox
        {
            get
            {
                var node = Addon->GetComponentNodeById(4);
                return node == null ? null : node->GetAsAtkComponentCheckBox();
            }
        }
        public AtkComponentButton* YesButton => Addon->GetComponentButtonById(6);
        public AtkComponentButton* NoButton => Addon->GetComponentButtonById(7);

        public override string AddonDescription { get; } = "Outfit glamour creation confirmation";

        public void Yes() => ClickButtonIfEnabled(YesButton);
        public void No() => ClickButtonIfEnabled(NoButton);
    }
}
