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
                // 第三道(String.Value 判空)原本漏在這一格 —— IsString() 內部雖然含
                // String.HasValue,但守衛的意圖要在呼叫點看得見,而不是靠另一個函式的實作細節。
                if(Addon->AtkValuesCount < 3 || !Addon->AtkValues[2].IsString() || Addon->AtkValues[2].String.Value == null)
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
        /// 高難任務完成計數(面板上「n/m」的左半)。讀不到或解析不出可信數字時回 <see langword="null"/>。
        /// 2026-08-02 實機崩潰的元兇:高難任務下這個索引的 AtkValue 型別不符,
        /// 原寫法無檢查直接解參考 → AccessViolation(try/catch 攔不到)。
        /// </summary>
        /// <remarks>
        /// 🔴 解析用 <see cref="ParseCounter"/>,<b>不再</b>用 <c>Regex.Replace(s, @"[^\d]", "")</c>。
        /// 那個寫法會把字串裡所有數字<b>連在一起</b>,「剩餘時間 25:10以上」變成 <b>2510</b> ——
        /// 一個看起來完全合理、卻根本不是計數的數字,消費端拿去比大小會靜默判錯。
        /// <br/><br/>
        /// 📌 <b>相容性(這是刻意保留的)</b>:<see cref="ParseCounter"/> 仍然容許前後綴文字
        /// (例如「進度 1」),所以原本要靠 regex fallback 才擠得出數字的<b>單一數字群</b>字串
        /// 照樣讀得到。只有「字串裡有兩組以上分開的數字」時行為才改變 —— 從假數字改成 null。
        /// 拿不準時請一併看 <see cref="CriticalScoreRaw"/> 的原字串。
        /// </remarks>
        public uint? CriticalScore
        {
            get
            {
                var rawValue = CriticalScoreRaw;
                if(rawValue == null)
                    return null;

                // Extract the left side of the slash
                return ParseCounter(rawValue.Split('/')[0]);
            }
        }

        /// <summary>
        /// <see cref="CriticalScore"/> 讀到的面板原字串,未經任何解析。
        /// 讀不到(AtkValues 未載入、型別不是字串、指標為空)時回 <see langword="null"/>。
        /// </summary>
        /// <remarks>
        /// 📌 存在的理由是<b>診斷</b>:<see cref="CriticalScore"/> 回 null 時,消費端光看 null
        /// 分不出「面板還沒載入」與「載入了但格式跟預期不一樣」。把原字串印出來才問得出後者。
        /// </remarks>
        public string? CriticalScoreRaw
        {
            get
            {
                if(Addon->AtkValuesCount < 6 || !Addon->AtkValues[5].IsString() || Addon->AtkValues[5].String.Value == null)
                    return null;

                return MemoryHelper.ReadSeStringNullTerminated((nint)Addon->AtkValues[5].String.Value)?.GetText();
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

        /// <summary>
        /// 把「計數」型的面板字串解析成數值。規則:整個字串裡<b>有且只有一組</b>連續數字時才採用它,
        /// 零組或兩組以上一律回 <see langword="null"/>。組內的千分位逗號會被略過。
        /// </summary>
        /// <remarks>
        /// 🔴 這條規則要解掉的是 <c>Regex.Replace(s, @"[^\d]", "")</c> 的<b>跨分隔符黏合</b>:
        /// 「25:10」被黏成 2510、「1／1」(全形斜線,台服字串很可能長這樣)被黏成 11。
        /// 黏出來的值不會拋例外、不會進 log,只會讓消費端靜默做出錯誤判斷。
        /// <br/><br/>
        /// 它比 <see cref="ParseScore"/> <b>寬鬆</b>(容許前後綴文字,「進度 1」讀得到 1),
        /// 比 regex 版<b>嚴格</b>(拒絕多組數字)。兩者刻意不共用:
        /// <list type="bullet">
        /// <item><see cref="ParseScore"/> 服務的是 <see cref="SilverScore"/>/<see cref="GoldScore"/>,
        /// 消費端拿去<b>比大小</b>,任何雜訊都可能造成提前交件 → 取最嚴。</item>
        /// <item>本函式服務的是 <see cref="CriticalScore"/>,消費端比的是<b>等於特定值</b>,
        /// 且既有行為已經容許前綴 —— 收得跟 ParseScore 一樣嚴會回退既有功能。</item>
        /// </list>
        /// </remarks>
        /// <param name="raw">面板原字串(可含前後綴文字)。</param>
        /// <returns>恰好一組連續數字時回該組數值;否則(含溢位)回 null。</returns>
        private static uint? ParseCounter(string? raw)
        {
            if(raw == null)
                return null;

            var digits = new StringBuilder(raw.Length);
            var groups = 0;
            var inGroup = false;

            foreach(var c in raw)
            {
                if(char.IsAsciiDigit(c))
                {
                    if(!inGroup)
                    {
                        groups++;
                        inGroup = true;
                    }
                    // 第二組以後不必再收,但要繼續掃完才知道總共幾組。
                    if(groups == 1)
                        digits.Append(c);
                    continue;
                }

                // 逗號夾在數字中間視為千分位,不切斷同一組;其餘字元(冒號、全形斜線、中文…)都切斷。
                if(c == ',' && inGroup)
                    continue;

                inGroup = false;
            }

            if(groups != 1)
                return null; // 0 組=沒有數字;≥2 組=分不出哪一組才是計數,不猜。

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
