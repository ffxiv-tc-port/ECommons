using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public unsafe partial class AddonMaster
{
    public class LotteryWeeklyRewardList : AddonMasterBase<AtkUnitBase>
    {
        public LotteryWeeklyRewardList(nint addon) : base(addon) { }
        public LotteryWeeklyRewardList(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 三重守衛(索引在界內／型別真的是字串／字串指標非空)後才解參考,
        /// 見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>。原寫法對 <c>String.Value</c>
        /// 無檢查直接解參考:型別不符時讀到的是別的欄位被當成指標 = AccessViolationException,
        /// 而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 讀不到時回<b>空字串</b>(維持既有的非可空簽章)。
        /// </remarks>
        public string Week => GenericHelpers.GetAtkValueText(Addon, 1);

        /// <remarks>索引出界時回 0,見 <see cref="GenericHelpers.GetAtkValueInt"/>。</remarks>
        public int WinningNumber => GenericHelpers.GetAtkValueInt(Addon, 5);

        public AtkComponentButton* CloseButton => Base->GetComponentButtonById(49);

        public void Close() => ClickButtonIfEnabled(CloseButton);

        public Reward[] Rewards
        {
            get
            {
                var ret = new Reward[5];
                for(var i = 0; i < ret.Length; i++)
                    ret[i] = new(this, i);
                return ret;
            }
        }

        public override string AddonDescription { get; } = "Jumbo Cactpot result window";

        public readonly struct Reward(LotteryWeeklyRewardList am, int index)
        {
            public bool Unk01 { get; init; } = GenericHelpers.GetAtkValueBool(am.Addon, 10 + 7 * index);

            /// <remarks>
            /// 🔴 原本只有 <see cref="ItemRewardName"/> 有型別檢查,同一個 struct 裡的
            /// <see cref="Place"/> 與 <see cref="Requirement"/> 完全沒有 —— 補到一致。
            /// 這裡維持非可空簽章,讀不到時回<b>空字串</b>(<see cref="ItemRewardName"/> 本來就是可空,
            /// 保留它的 null 語意)。
            /// </remarks>
            public string Place { get; init; } = GenericHelpers.GetAtkValueText(am.Addon, 11 + 7 * index);
            public int MGPReward { get; init; } = GenericHelpers.GetAtkValueInt(am.Addon, 12 + 7 * index);
            public int ItemRewardId { get; init; } = GenericHelpers.GetAtkValueInt(am.Addon, 13 + 7 * index);
            public int? ItemRewardIconId { get; init; } = GenericHelpers.GetAtkValueType(am.Addon, 14 + 7 * index) == 0 ? null : GenericHelpers.GetAtkValueInt(am.Addon, 14 + 7 * index);
            public string? ItemRewardName { get; init; } = GenericHelpers.GetAtkValueTextOrNull(am.Addon, 15 + 7 * index);

            /// <inheritdoc cref="Place"/>
            public string Requirement { get; init; } = GenericHelpers.GetAtkValueText(am.Addon, 16 + 7 * index);
        }

        // TODO: Particpant Rewards struct
    }
}