using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Globalization;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    /// <summary>
    /// Mission information screen
    /// Can be viewed post you grabbing a moon mission
    /// </summary>
    public unsafe partial class WKSMissionInfomation : AddonMasterBase<AtkUnitBase>
    {
        public WKSMissionInfomation(nint addon) : base(addon) { }
        public WKSMissionInfomation(void* addon) : base(addon) { }

        /// <summary>
        /// 任務名。讀不到(AtkValues 未載入、型別不是字串、指標為空)時回空字串。
        /// 與 CurrentScore 同款加固:高難任務等情境下部分 AtkValue 不是字串,
        /// 無檢查直接解參考會拿垃圾指標造成 AccessViolation(實機崩潰 2026-08-02)。
        /// </summary>
        public string Name
        {
            get
            {
                if(Addon->AtkValuesCount < 1 || !Addon->AtkValues[0].IsString() || Addon->AtkValues[0].String.Value == null)
                    return "";

                return MemoryHelper
                    .ReadSeStringNullTerminated((nint)Addon->AtkValues[0].String.Value)
                    ?.GetText() ?? "";
            }
        }

        /// <summary>
        /// 目前任務分數。讀不到(AtkValues 未載入、型別不是字串、或解析失敗)時回 null,
        /// 讓呼叫端能區分「還沒有資料」與「分數是 0」。
        /// </summary>
        /// <remarks>
        /// 移植自上游 NightmareXIV/ECommons 的加固:c2a54c5(改 uint? + 邊界檢查 + null 檢查)
        /// 與 2d8d2f4(補 AtkValue 型別檢查)。原寫法對 AtkValues[2].String.Value 無檢查直接
        /// 解參考,型別不符時讀到的是垃圾指標。上游的邊界檢查寫 AtkValuesCount &lt; 2,
        /// 但這裡讀的是索引 2,正確下界是 3 —— 半套邊界檢查是本艦隊踩過的雷,這裡取正確值。
        /// </remarks>
        public uint? CurrentScore
        {
            get
            {
                if(Addon->AtkValuesCount < 3 || !Addon->AtkValues[2].IsString())
                    return null;

                var rawValue = MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[2].String.Value);
                if(rawValue == null)
                    return null;

                // Number coversion test #1.
                if(uint.TryParse(rawValue.TextValue, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
                    return result;

                // Fallback: if the first test fails
                var cleanedValue = System.Text.RegularExpressions.Regex.Replace(rawValue.TextValue, @"[^\d]", "");
                if(uint.TryParse(cleanedValue, out result))
                    return result;

                return null; // 解析失敗:回 null 而不是假的 0
            }
        }

        /// <summary>
        /// 銀章門檻。讀不到時回 null —— 不能回 0:門檻 0 會讓「分數 >= 門檻」恆真,
        /// 造成提前交付。呼叫端拿到 null 應視為「資料未就緒」跳過本輪判斷。
        /// </summary>
        public uint? SilverScore
        {
            get
            {
                if(Addon->AtkValuesCount < 4 || !Addon->AtkValues[3].IsString() || Addon->AtkValues[3].String.Value == null)
                    return null;

                var rawValue = MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[3].String.Value)?.GetText();
                if(rawValue == null)
                    return null;

                // Number coversion test #1.
                if(uint.TryParse(rawValue, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
                    return result;

                // Fallback: if the first test fails
                var cleanedValue = System.Text.RegularExpressions.Regex.Replace(rawValue, @"[^\d]", "");
                if(uint.TryParse(cleanedValue, out result))
                    return result;

                return null; // 解析失敗:回 null 而不是假的 0
            }
        }

        /// <summary>金章門檻。讀不到時回 null,理由同 <see cref="SilverScore"/>。</summary>
        public uint? GoldScore
        {
            get
            {
                if(Addon->AtkValuesCount < 5 || !Addon->AtkValues[4].IsString() || Addon->AtkValues[4].String.Value == null)
                    return null;

                var rawValue = MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[4].String.Value)?.GetText();
                if(rawValue == null)
                    return null;

                // Number coversion test #1.
                if(uint.TryParse(rawValue, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
                    return result;

                // Fallback: if the first test fails
                var cleanedValue = System.Text.RegularExpressions.Regex.Replace(rawValue, @"[^\d]", "");
                if(uint.TryParse(cleanedValue, out result))
                    return result;

                return null; // 解析失敗:回 null 而不是假的 0
            }
        }

        /// <summary>
        /// 高難任務完成計數。讀不到時回 null。
        /// 2026-08-02 實機崩潰的元兇:高難任務下這個索引的 AtkValue 型別不符,
        /// 原寫法無檢查直接解參考 → AccessViolation(try/catch 攔不到)。
        /// </summary>
        public uint? CriticalScore
        {
            get
            {
                if(Addon->AtkValuesCount < 6 || !Addon->AtkValues[5].IsString() || Addon->AtkValues[5].String.Value == null)
                    return null;

                var rawValue = MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[5].String.Value)?.GetText();
                if(rawValue == null)
                    return null;

                // Extract the left side of the slash
                var leftSide = rawValue.Split('/')[0].Trim();

                // Number conversion test #1.
                if(uint.TryParse(leftSide, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
                    return result;

                // Fallback: if the first test fails
                var cleanedValue = System.Text.RegularExpressions.Regex.Replace(leftSide, @"[^\d]", "");
                if(uint.TryParse(cleanedValue, out result))
                    return result;

                return null; // 解析失敗:回 null 而不是假的 0
            }
        }

        public AtkComponentButton* CosmoPouchButton => Addon->GetComponentButtonById(26);
        public AtkComponentButton* CosmoCraftingLogButton => Addon->GetComponentButtonById(27);
        public AtkComponentButton* StellerReductionButton => Addon->GetComponentButtonById(28);
        public AtkComponentButton* ReportResultsButton => Addon->GetComponentButtonById(29);
        public AtkComponentButton* AbandonMissionButton => Addon->GetComponentButtonById(30);

        public void CosmoPouch() => ClickButtonIfEnabled(CosmoPouchButton);
        public void CosmoCraftingLog() => ClickButtonIfEnabled(CosmoCraftingLogButton);
        public void StellerReduction() => ClickButtonIfEnabled(StellerReductionButton);
        public void Report() => ClickButtonIfEnabled(ReportResultsButton);
        public void Abandon() => ClickButtonIfEnabled(AbandonMissionButton);

        public override string AddonDescription => "Cosmic Exploration Mission Information";
    }
}
