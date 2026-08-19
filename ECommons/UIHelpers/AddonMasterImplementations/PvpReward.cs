using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    /// <summary>
    /// PvP Series rewards addon
    /// </summary>
    public unsafe class PvpReward : AddonMasterBase<AtkUnitBase>
    {
        public PvpReward(nint addon) : base(addon) { }
        public PvpReward(void* addon) : base(addon) { }

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public uint CurrentLevelXp => GenericHelpers.GetAtkValueUInt(Addon, 10);
        public uint TotalLevelXp => GenericHelpers.GetAtkValueUInt(Addon, 11);

        public AtkComponentButton* CloseButton => Addon->GetComponentButtonById(124);

        public override string AddonDescription { get; } = "PvP Series rewards window";

        public void Close() => ClickButtonIfEnabled(CloseButton);
    }
}
