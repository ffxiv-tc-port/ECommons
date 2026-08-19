using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class GatheringMasterpiece : AddonMasterBase<AddonGatheringMasterpiece>
    {
        public GatheringMasterpiece(nint addon) : base(addon) { }
        public GatheringMasterpiece(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 節點取得器合法回 null,而 <c>->NodeText</c> 是內嵌值(偏移 0xC0):
        /// 對 null 節點取它就是解參考 null+0xC0 = AccessViolation,而 AVE 是
        /// corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。取不到時回空字串。
        /// </remarks>
        public string ItemName
        {
            get
            {
                var node = Addon->ItemName;
                return node == null ? string.Empty : node->NodeText.GetText();
            }
        }
        public uint ItemID => Addon->AtkUnitBase.AtkValues[2].UInt;

        public int CurrentCollectability => Addon->AtkUnitBase.AtkValues[13].Int;
        public int MaxCollectability => Addon->AtkUnitBase.AtkValues[14].Int;
        public uint MinCollectability => Addon->AtkUnitBase.AtkValues[65].UInt;
        public uint MidCollectability => Addon->AtkUnitBase.AtkValues[66].UInt;
        public uint HighCollectability => Addon->AtkUnitBase.AtkValues[67].UInt;

        public uint CurrentIntegrity => Addon->AtkUnitBase.AtkValues[62].UInt;

        public uint TotalIntegrity => Addon->AtkUnitBase.AtkValues[63].UInt;

        public uint GatherChance => Addon->AtkUnitBase.AtkValues[18].UInt;

        public int ScourPower => Addon->AtkUnitBase.AtkValues[48].Int;
        public int BrazenPowerMin => Addon->AtkUnitBase.AtkValues[49].Int;
        public int BrazenPowerMax => Addon->AtkUnitBase.AtkValues[50].Int;
        public int MeticulousPower => Addon->AtkUnitBase.AtkValues[51].Int;

        /// <remarks>
        /// 🔴 <c>GetComponentNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>GetAsAtkComponent*()</c>
        /// 是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼,
        /// 當場 AccessViolation;AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到就回 null;既有消費端 <c>ClickButtonIfEnabled</c>/<c>ClickCheckboxIfEnabled</c>
        /// 開頭都已判空,所以取得到時行為完全照舊。
        /// </remarks>
        private AtkComponentCheckBox* CheckBoxById(uint id)
        {
            var node = Addon->GetComponentNodeById(id);
            return node == null ? null : node->GetAsAtkComponentCheckBox();
        }

        public AtkComponentCheckBox* ScrutinyCheckBox => CheckBoxById(177);
        public AtkComponentCheckBox* CollectorsIntuitionCheckBox => CheckBoxById(178);
        public AtkComponentButton* HelpButton => Addon->GetComponentButtonById(182);
        public AtkComponentButton* ReturnButton => Addon->GetComponentButtonById(183);

        public override string AddonDescription { get; } = "Collectables gathering window";

        public void Help() => ClickButtonIfEnabled(HelpButton);
        public void Return() => ClickButtonIfEnabled(ReturnButton);
    }
}
