using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class WorldTravelFinderStatus : AddonMasterBase<AtkUnitBase>
    {
        public WorldTravelFinderStatus(nint addon) : base(addon) { }
        public WorldTravelFinderStatus(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 三重守衛(索引在界內／型別真的是字串／字串指標非空)後才解參考,
        /// 見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>。原寫法對 <c>String.Value</c>
        /// 無檢查直接解參考:型別不符時讀到的是別的欄位被當成指標 = AccessViolationException,
        /// 而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 讀不到時回<b>空字串</b>(維持既有的非可空簽章)。
        /// </remarks>
        public string StartingWorldString => GenericHelpers.GetAtkValueText(Addon, 1);

        /// <inheritdoc cref="StartingWorldString"/>
        public string DestinationWorldString => GenericHelpers.GetAtkValueText(Addon, 2);

        /// <inheritdoc cref="StartingWorldString"/>
        public string PositionInQueueString => GenericHelpers.GetAtkValueText(Addon, 3);

        /// <inheritdoc cref="StartingWorldString"/>
        public string TimeElapsedString => GenericHelpers.GetAtkValueText(Addon, 4);

        /// <inheritdoc cref="StartingWorldString"/>
        public string TimeRemainingString => GenericHelpers.GetAtkValueText(Addon, 5);
        /* TODO: fix or delete
        public World? StartingWorld => GenericHelpers.FindRow<World>(x => !string.IsNullOrEmpty(x!.Name) && x.Name == StartingWorldString);
        public World? DestinationWorld => GenericHelpers.FindRow<World>(x => !string.IsNullOrEmpty(x!.Name) && x.Name == DestinationWorldString);*/

        public AtkComponentButton* CancelButton => Addon->GetComponentButtonById(13);

        public override string AddonDescription => "In-game world travel status window";

        public void Cancel() => ClickButtonIfEnabled(CancelButton);
    }
}
