using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Globalization;
using System.Text;

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

                return ParseScore(rawValue.TextValue); // 解析失敗:回 null 而不是假的 0
            }
        }

        /// <summary>
        /// 銀章門檻。讀不到時回 null —— 不能回 0:門檻 0 會讓「分數 >= 門檻」恆真,
        /// 造成提前交付。呼叫端拿到 null 應視為「資料未就緒」跳過本輪判斷。
        /// </summary>
        /// <remarks>
        /// 🔴 <b>時間型任務這一格不是分數</b>,而是「剩餘時間 25:10以上」這種文字。
        /// 這種字串一律回 null(見 <see cref="ParseScore"/>),呼叫端必須自己走時間型的判斷路徑,
        /// <b>不要</b>把回傳值當分數比大小。
        /// </remarks>
        public uint? SilverScore
        {
            get
            {
                if(Addon->AtkValuesCount < 4 || !Addon->AtkValues[3].IsString() || Addon->AtkValues[3].String.Value == null)
                    return null;

                var rawValue = MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[3].String.Value)?.GetText();
                if(rawValue == null)
                    return null;

                return ParseScore(rawValue); // 解析失敗:回 null 而不是假的 0
            }
        }

        /// <summary>金章門檻。讀不到時回 null,理由同 <see cref="SilverScore"/>(含時間型任務的陷阱)。</summary>
        public uint? GoldScore
        {
            get
            {
                if(Addon->AtkValuesCount < 5 || !Addon->AtkValues[4].IsString() || Addon->AtkValues[4].String.Value == null)
                    return null;

                var rawValue = MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[4].String.Value)?.GetText();
                if(rawValue == null)
                    return null;

                return ParseScore(rawValue); // 解析失敗:回 null 而不是假的 0
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

        /// <summary>
        /// 把面板字串解析成分數。<b>只接受「純分數」</b>:去掉千分位逗號與空白之後,
        /// 剩下的字元必須全部是數字,否則回 <see langword="null"/>。
        /// </summary>
        /// <remarks>
        /// 🔴 這裡刻意<b>不</b>用 <c>Regex.Replace(s, @"[^\d]", "")</c> 把數字硬抽出來。
        /// 宇宙探索的任務分兩型,<b>時間型</b>任務的門檻欄位放的是文字「剩餘時間 25:10以上」,
        /// 硬抽會壓成 <b>2510</b> —— 一個看起來完全合理、卻根本不是分數的數字。
        /// 消費端拿它去比大小不會拋例外、log 裡也不會留痕跡,只會<b>靜默</b>做出錯誤判斷
        /// (例如誤判已達金章門檻而提前交件)。寧可回 null 讓呼叫端知道「這格讀不出分數」。
        /// <br/><br/>
        /// 📌 <b>相容性</b>:凡是原本第一段 <c>uint.TryParse(NumberStyles.AllowThousands)</c> 就會成功的
        /// 字串,這裡一律照樣成功(本函式對逗號位置更寬鬆,且額外容許前後空白)。
        /// 所以行為差異<b>只發生在</b>「原本得靠 regex fallback 才擠得出數字」的字串上 ——
        /// 而那些正是不該相信的那些。
        /// </remarks>
        /// <param name="raw">面板原字串。</param>
        /// <returns>純分數時回數值;格式不符、沒有任何數字、或超出 <see cref="uint"/> 範圍時回 null。</returns>
        private static uint? ParseScore(string? raw)
        {
            if(raw == null)
                return null;

            var digits = new StringBuilder(raw.Length);
            foreach(var c in raw)
            {
                // 千分位逗號與空白是分數的合法裝飾,略過不看。
                if(c == ',' || char.IsWhiteSpace(c))
                    continue;

                // 冒號、中文、百分號…只要出現一個,就不是純分數格式。
                if(!char.IsAsciiDigit(c))
                    return null;

                digits.Append(c);
            }

            if(digits.Length == 0)
                return null;

            return uint.TryParse(digits.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var result)
                ? result
                : null; // 溢位
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
