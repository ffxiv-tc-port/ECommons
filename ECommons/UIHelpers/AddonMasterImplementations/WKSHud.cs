using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Text.RegularExpressions;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    /// <summary>
    /// Space Exploration main window (always visible on the area)
    /// </summary>
    public unsafe partial class WKSHud : AddonMasterBase<AtkUnitBase>
    {
        public WKSHud(nint addon) : base(addon) { }
        public WKSHud(void* addon) : base(addon) { }

        public AtkComponentButton* StellerMissionsButton => ButtonByIdOrNull(7);
        public AtkComponentButton* MechOpsButton => ButtonByIdOrNull(8);
        public AtkComponentButton* StellerSuccessButton => ButtonByIdOrNull(9);
        public AtkComponentButton* InfrastructorIndexButton => ButtonByIdOrNull(10);
        public AtkComponentButton* CosmicResearchButton => ButtonByIdOrNull(11);
        public AtkComponentButton* CosmicClassTrackerButton => ButtonByIdOrNull(12);

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。
        /// </remarks>
        public int CosmoCredit => GenericHelpers.GetAtkValueInt(Addon, 2);
        public int LunarCredit => GenericHelpers.GetAtkValueInt(Addon, 6);

        public override string AddonDescription => "Cosmic Exploration Main Hud Menu";

        public void Mission() => ClickButtonIfEnabled(StellerMissionsButton);
        public void Mech() => ClickButtonIfEnabled(MechOpsButton);
        public void Steller() => ClickButtonIfEnabled(StellerSuccessButton);
        public void Infrastructor() => ClickButtonIfEnabled(InfrastructorIndexButton);
        public void Research() => ClickButtonIfEnabled(CosmicResearchButton);
        public void ClassTracker() => ClickButtonIfEnabled(CosmicClassTrackerButton);

        [GeneratedRegex(@"\d+")]
        private static partial Regex ExtractNumber();
    }
}
