using FFXIVClientStructs.FFXIV.Component.GUI;
namespace ECommons.UIHelpers.AddonMasterImplementations;

public partial class AddonMaster
{
    public unsafe partial class MJIHud : AddonMasterBase<AtkUnitBase>
    {
        public MJIHud(nint addon) : base(addon) { }
        public MJIHud(void* addon) : base(addon) { }

        public override string AddonDescription { get; } = "Island Sanctuary Main Hud";

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public uint SanctuaryRank => GenericHelpers.GetAtkValueUInt(Addon, 11);
        public uint CurrentIslandXP => GenericHelpers.GetAtkValueUInt(Addon, 12);
        public uint NextIslandLevelXP => GenericHelpers.GetAtkValueUInt(Addon, 13);
        public uint IslandersCowrie => GenericHelpers.GetAtkValueUInt(Addon, 14);
        public uint SeafarersCowrie => GenericHelpers.GetAtkValueUInt(Addon, 17);

        public AtkComponentButton* IsleventoryButton => Addon->GetComponentButtonById(20);
        public AtkComponentButton* SanctuaryCraftingLogButton => Addon->GetComponentButtonById(21);
        public AtkComponentButton* SanctuaryGatheringLogButton => Addon->GetComponentButtonById(22);
        public AtkComponentButton* ManageHideawayButton => Addon->GetComponentButtonById(23);
        public AtkComponentButton* MaterialAllocationButton => Addon->GetComponentButtonById(24);
        public AtkComponentButton* ManageMinionButton => Addon->GetComponentButtonById(25);
        public AtkComponentButton* ManageFurnishingButton => Addon->GetComponentButtonById(26);
        public AtkComponentButton* GuideButton => Addon->GetComponentButtonById(27);


        public void Isleventory() => ClickButtonIfEnabled(IsleventoryButton);
        public void SanctuaryCraftingLog() => ClickButtonIfEnabled(SanctuaryCraftingLogButton);
        public void SanctuaryGatheringLog() => ClickButtonIfEnabled(SanctuaryGatheringLogButton);
        public void ManageHideaway() => ClickButtonIfEnabled(ManageHideawayButton);
        public void MaterialAllocation() => ClickButtonIfEnabled(MaterialAllocationButton);
        public void ManageMinions() => ClickButtonIfEnabled(ManageHideawayButton);
        public void ManageFurnishing() => ClickButtonIfEnabled(ManageFurnishingButton);
        public void Guide() => ClickButtonIfEnabled(GuideButton);
    }
}
