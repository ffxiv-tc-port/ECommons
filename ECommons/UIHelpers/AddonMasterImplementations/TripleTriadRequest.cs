using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static ECommons.GenericHelpers;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class TripleTriadRequest : AddonMasterBase<AtkUnitBase>
    {
        public TripleTriadRequest(nint addon) : base(addon) { }
        public TripleTriadRequest(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 三重守衛(索引在界內／型別真的是字串／字串指標非空)後才解參考,
        /// 見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>。原寫法對 <c>String.Value</c>
        /// 無檢查直接解參考:型別不符時讀到的是別的欄位被當成指標 = AccessViolationException,
        /// 而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 讀不到時回<b>空字串</b>(維持既有的非可空簽章)。
        /// </remarks>
        public string Opponent => GenericHelpers.GetAtkValueText(Addon, 7);

        /// <remarks>索引出界時回 0,見 <see cref="GenericHelpers.GetAtkValueInt"/>。</remarks>
        public int CurrentMGP => GenericHelpers.GetAtkValueInt(Addon, 9);

        /// <inheritdoc cref="CurrentMGP"/>
        public int RegionalRule1 => GenericHelpers.GetAtkValueInt(Addon, 102);

        /// <inheritdoc cref="CurrentMGP"/>
        public int RegionalRule2 => GenericHelpers.GetAtkValueInt(Addon, 103);

        /// <inheritdoc cref="CurrentMGP"/>
        public int MatchRule1 => GenericHelpers.GetAtkValueInt(Addon, 104);

        /// <inheritdoc cref="CurrentMGP"/>
        public int MatchRule2 => GenericHelpers.GetAtkValueInt(Addon, 105);
        //TODO: fix
        //public List<TripleTriadRule> RegionalRules => [GetRow<TripleTriadRule>((uint)RegionalRule1), GetRow<TripleTriadRule>((uint)RegionalRule2)];
        //public List<TripleTriadRule> MatchRules => [GetRow<TripleTriadRule>((uint)MatchRule1), GetRow<TripleTriadRule>((uint)MatchRule2)];

        /// <inheritdoc cref="CurrentMGP"/>
        public int MatchFee => GenericHelpers.GetAtkValueInt(Addon, 111);

        /// <inheritdoc cref="CurrentMGP"/>
        public uint MGPReward => GenericHelpers.GetAtkValueUInt(Addon, 112);

        public AtkComponentButton* ChallengeButton => Addon->GetComponentButtonById(41);
        public AtkComponentButton* QuitButton => Addon->GetComponentButtonById(42);

        public override string AddonDescription { get; } = "Triple triad challenge window";

        public void Challenge() => ClickButtonIfEnabled(ChallengeButton);
        public void Quit() => ClickButtonIfEnabled(QuitButton);
    }
}
