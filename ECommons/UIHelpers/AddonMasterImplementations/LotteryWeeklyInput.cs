using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public unsafe partial class AddonMaster
{
    public class LotteryWeeklyInput : AddonMasterBase<AtkUnitBase>
    {
        public LotteryWeeklyInput(nint addon) : base(addon) { }
        public LotteryWeeklyInput(void* addon) : base(addon) { }

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public int Week => GenericHelpers.GetAtkValueInt(Addon, 0);
        public bool Unk03 => GenericHelpers.GetAtkValueBool(Addon, 3);
        public int Unk04 => GenericHelpers.GetAtkValueInt(Addon, 4);
        public int Unk05 => GenericHelpers.GetAtkValueInt(Addon, 5);

        public AtkComponentButton* PurchaseButton => Base->GetComponentButtonById(31);
        public AtkComponentButton* RandomButton => Base->GetComponentButtonById(32);

        public override string AddonDescription { get; } = "Jumbo Cactpot ticket purchase window";

        public void Purchase() => ClickButtonIfEnabled(PurchaseButton);
        public void Random() => ClickButtonIfEnabled(RandomButton);
    }
}