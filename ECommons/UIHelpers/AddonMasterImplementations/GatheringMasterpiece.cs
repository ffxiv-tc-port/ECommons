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
        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public uint ItemID => GenericHelpers.GetAtkValueUInt(Base, 2);

        public int CurrentCollectability => GenericHelpers.GetAtkValueInt(Base, 13);
        public int MaxCollectability => GenericHelpers.GetAtkValueInt(Base, 14);
        public uint MinCollectability => GenericHelpers.GetAtkValueUInt(Base, 65);
        public uint MidCollectability => GenericHelpers.GetAtkValueUInt(Base, 66);
        public uint HighCollectability => GenericHelpers.GetAtkValueUInt(Base, 67);

        public uint CurrentIntegrity => GenericHelpers.GetAtkValueUInt(Base, 62);

        public uint TotalIntegrity => GenericHelpers.GetAtkValueUInt(Base, 63);

        public uint GatherChance => GenericHelpers.GetAtkValueUInt(Base, 18);

        public int ScourPower => GenericHelpers.GetAtkValueInt(Base, 48);
        public int BrazenPowerMin => GenericHelpers.GetAtkValueInt(Base, 49);
        public int BrazenPowerMax => GenericHelpers.GetAtkValueInt(Base, 50);
        public int MeticulousPower => GenericHelpers.GetAtkValueInt(Base, 51);

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
