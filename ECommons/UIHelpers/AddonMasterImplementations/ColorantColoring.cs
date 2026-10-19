using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public unsafe partial class AddonMaster
{
    /// <summary>
    /// Item dyeing addon
    /// </summary>
    public class ColorantColoring : AddonMasterBase<AtkUnitBase>
    {
        public ColorantColoring(nint addon) : base(addon) { }
        public ColorantColoring(void* addon) : base(addon) { }

        public uint ItemId => GenericHelpers.GetAtkValueUInt(Addon, 2);
        public int ItemIconId => GenericHelpers.GetAtkValueInt(Addon, 3);

        /// <remarks>
        /// 🔴 三重守衛(索引在界內／型別真的是字串／字串指標非空)後才解參考,
        /// 見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>(缺任何一道都是攔不到的 AccessViolationException)。
        /// 讀不到時回<b>空字串</b>(維持既有的非可空簽章)。
        /// </remarks>
        public string ItemName => GenericHelpers.GetAtkValueText(Addon, 4);

        public AtkComponentButton* ApplyButton => Base->GetComponentButtonById(68);
        public AtkComponentButton* SelectAnotherButton => Base->GetComponentButtonById(69);

        public override string AddonDescription { get; } = "Item dyeing window";

        public void Apply() => ClickButtonIfEnabled(ApplyButton);
        public void SelectAnother() => ClickButtonIfEnabled(SelectAnotherButton);
    }
}