using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Data;
using Lumina.Excel.Sheets;
using System.Linq;
using SheetContentType = Lumina.Excel.Sheets.ContentType;
using TerritoryHelper = ECommons.TerritoryName;

#nullable disable

namespace ECommons.GameHelpers;

/// <summary>
///     Primary types of actual (in regard to combat) content in the game.
/// </summary>
/// <seealso cref="Content.DetermineContentType" />
public enum ContentType
{
    Unknown,
    /// <summary>
    ///     This encompasses anything that isn't in a roulette or a field operation,
    ///     quest instances, actual over world content, housing, for-fun content,
    ///     etc.
    /// </summary>
    OverWorld,
    PVP,
    Dungeon,
    DeepDungeon,
    Variant,
    /// This includes Criterion Savage
    Criterion,
    Trial,
    /// Bozja, Eureka, Diadem, etc.
    FieldOperations,
    /// Delubrum Reginae, Dalriada, etc.
    FieldRaid,
    ARaid,
    Raid,
}

/// <summary>
///     All the difficulties of content in the game.
/// </summary>
/// <seealso cref="Content.DetermineContentDifficulty" />
public enum ContentDifficulty
{
    Unknown,
    /// <summary>
    ///     This encompasses anything that doesn't otherwise have an explicit
    ///     difficulty, or is the lowest difficulty, variant dungeons, field raids,
    ///     etc.
    /// </summary>
    Normal,
    Hard,
    Unreal,
    /// Only Delubrum Reginae Savage
    FieldRaidsSavage,
    Extreme,
    Chaotic,
    Criterion,
    Savage,
    CriterionSavage,
    Ultimate,
}

/// <summary>
///     Organization of the biggest pieces of data about the content the user is
///     currently engaged in.
/// </summary>
/// <remarks>
///     This entire class is skewed towards identifying 'standard' PvE combat content,
///     as in content in roulettes and the higher end.
/// </remarks>
public static class Content
{
    /// <summary>
    ///     The ID of the current territory the player is in.
    /// </summary>
    public static uint TerritoryID => Svc.ClientState.TerritoryType;

    /// <summary>
    ///     The result of the TerritoryName builder.
    /// </summary>
    /// <seealso cref="TerritoryHelper.GetTerritoryName" />
    private static string TerritoryNameResult =>
        TerritoryHelper.GetTerritoryName(TerritoryID);

    /// <summary>
    ///     Whether the TerritoryName came out successfully from the builder.
    /// </summary>
    /// <seealso cref="TerritoryNameResult" />
    /// <seealso cref="TerritoryName" />
    private static bool TerritoryNameResolved =>
        TerritoryNameResult.Contains('|');

    /// <summary>
    ///     The zone name of the current territory the player is in.
    /// </summary>
    /// <value><c>null</c> when not resolved.</value>
    /// <seealso cref="TerritoryHelper.GetTerritoryName" />
    /// <seealso cref="TerritoryNameResolved" />
    /// <seealso cref="TerritoryNameResult" />
    public static string? TerritoryName =>
        TerritoryNameResolved
            ? TerritoryNameResult.Split('|')[1].Split('(')[0].Trim()
            : null;

    /// <summary>
    ///     The Sheet row for the current <see cref="TerritoryType" />.
    /// </summary>
    public static TerritoryType? TerritoryTypeRow =>
        Svc.Data.Excel.GetSheet<TerritoryType>(Language.English)!
            .GetRowOrDefault(Svc.ClientState.TerritoryType);

    /// <summary>
    ///     The ID of the current map the player is in.
    /// </summary>
    public static uint MapID =>
        Svc.ClientState.MapId;

    /// <summary>
    ///     The intended use of the current territory the player is in.
    /// </summary>
    /// <seealso cref="TerritoryIntendedUseEnum" />
    public static TerritoryIntendedUseEnum? TerritoryIntendedUse
    {
        get
        {
            var intendedUseRow = TerritoryTypeRow?
                .TerritoryIntendedUse
                .ValueNullable?.RowId;

            if(intendedUseRow != null)
                return (TerritoryIntendedUseEnum)intendedUseRow;

            return null;
        }
    }

    /// <summary>
    ///     The Sheet row for the current <see cref="ContentFinderCondition" />.
    /// </summary>
    public static ContentFinderCondition? ContentFinderConditionRow =>
        TerritoryTypeRow?.ContentFinderCondition.ValueNullable;

    /// <summary>
    ///     The content name of the current territory the player is in.
    /// </summary>
    /// <value>
    ///     Falls back to <see cref="TerritoryName" /> when
    ///     <see cref="ContentFinderCondition">CFC Data</see> is not resolved.<br />
    ///     <c>null</c> when <see cref="TerritoryName" /> is also not
    ///     resolved.
    /// </value>
    /// <seealso cref="ContentFinderCondition" />
    /// <seealso cref="ContentFinderConditionRow" />
    public static string? ContentName =>
        TerritoryNameResolved
            ? ContentFinderConditionRow != null
                ? ContentFinderConditionRow?.Name.ToString()
                : TerritoryName
            : null;

    /// <summary>
    ///     If the content allows Undersized (Unrestricted) Parties.
    /// </summary>
    public static bool? AllowUndersized =>
        ContentFinderConditionRow?.AllowUndersized;

    /// <summary>
    ///     If the content is listed under High-End Content in the Duty Finder.
    /// </summary>
    public static bool? HighEndDuty =>
        ContentFinderConditionRow?.HighEndDuty;

    /// <summary>
    ///     Whether the difficulty was found in the <see cref="ContentName" />.
    /// </summary>
    private static bool ContentDifficultyFromNameResolved =>
        ContentDifficultyFromName is not null;

    /// <summary>
    ///     The title case difficulty of the content as found in the
    ///     <see cref="ContentName" />.
    /// </summary>
    /// <value>
    ///     <c>null</c> when not
    ///     <see cref="ContentDifficultyFromNameResolved">resolved</see> or when
    ///     <see cref="ContentFinderConditionRow" /> is null.
    /// </value>
    /// <remarks>
    ///     台服(TC)注意:我們的 Dalamud fork 的 Lumina 會把指定語言的 Excel
    ///     請求靜默改成 client 語言,因此本類以 <c>Language.English</c> 取得的表
    ///     實際上是繁中資料,英文括號後綴(" (savage" 等)永遠比對不到,
    ///     需另以繁中命名規則判定(見 <see cref="ContentDifficultyFromNameTC" />)。
    /// </remarks>
    public static string? ContentDifficultyFromName
    {
        get
        {
            if(ContentFinderConditionRow is null)
                return null;

            var contentName = ContentFinderConditionRow.Value.Name.ToString();
            var lowered = contentName.ToLower();
            if(lowered.Contains(" (hard") ||
               lowered.Contains(" (extreme") ||
               lowered.Contains(" (savage"))
                return contentName.Split('(').Last().TrimEnd(')').Trim();

            return ContentDifficultyFromNameTC(contentName);
        }
    }

    /// <summary>
    ///     台服 CFC 名稱的難度關鍵字判定,回傳與英文分支相同的難度 token。<br />
    ///     零式=Savage(7.20 dump 實查:60 個零式 raid 全中、零誤中;舊零式
    ///     HighEndDuty=false,無法數值判定);「極 」/「極王」前綴與
    ///     「究極幻想/蒼天幻想/終極之戰」(吟遊詩人的敘事詩系列)=Extreme;
    ///     「真 」前綴=Hard。<br />
    ///     刻意不用 Contains("極"):會誤中「究極武器破壞作戰」(普通難度)與
    ///     「極惡之人木枯」(單人任務戰鬥);也不用 Contains("幻想"):會誤中
    ///     「迦巴勒幻想圖書館」等迷宮。台服困難迷宮無命名標記(如「騷亂坑道
    ///     銅鈴銅山」),維持 Normal——與消費端的難度分組(Casual/SoftCore)等價。
    /// </summary>
    private static string? ContentDifficultyFromNameTC(string contentName)
    {
        if(contentName.Contains("零式"))
            return "Savage";
        if(contentName.StartsWith("極 ") || contentName.StartsWith("極王") ||
           contentName.StartsWith("究極幻想") || contentName.StartsWith("蒼天幻想") ||
           contentName == "終極之戰")
            return "Extreme";
        if(contentName.StartsWith("真 "))
            return "Hard";
        return null;
    }

    /// <summary>
    ///     The Sheet row for the current <see cref="InstanceContent" />.
    /// </summary>
    public static InstanceContent? InstanceContentRow
    {
        get
        {
            var instanceContentRow = ContentFinderConditionRow?.Content.RowId;

            if(instanceContentRow != null)
                return Svc.Data.Excel.GetSheet<InstanceContent>(Language.English)!
                    .GetRowOrDefault((uint)instanceContentRow);

            return null;
        }
    }

    /// <summary>
    ///     The number of minutes the current piece of content is restricted to.
    /// </summary>
    public static ushort? TimeLimit =>
        InstanceContentRow?.TimeLimitmin;

    /// <summary>
    ///     The Sheet row for the current <see cref="ContentType" />.
    /// </summary>
    public static SheetContentType? ContentTypeRow
    {
        get
        {
            var contentTypeRowId = ContentFinderConditionRow?.ContentType.RowId;

            if(contentTypeRowId != null)
                return Svc.Data.Excel.GetSheet<SheetContentType>(Language.English)!
                    .GetRowOrDefault((uint)contentTypeRowId);

            return null;
        }
    }

    /// <summary>
    ///     The Row ID of the current <see cref="SheetContentType" />.
    /// </summary>
    private static uint? ContentTypeRowId =>
        ContentTypeRow?.RowId;

    /// <summary>
    ///     The name of the current <see cref="SheetContentType" />.
    /// </summary>
    public static string? ContentTypeName =>
        ContentTypeRowId is not null && ContentTypeRowId != 0
            ? ContentTypeRow?.Name.ToString()
            : ContentTypeRowId == 0
                ? "OverWorld"
                : null;

    /// <summary>
    ///     The determined <see cref="ContentType" /> of the current content.
    /// </summary>
    /// <seealso cref="DetermineContentType" />
    public static ContentType? ContentType => DetermineContentType();

    /// <summary>
    ///     The determined <see cref="ContentDifficulty" /> of the current content.
    /// </summary>
    /// <seealso cref="DetermineContentDifficulty" />
    public static ContentDifficulty? ContentDifficulty =>
        DetermineContentDifficulty();

    /// <summary>
    ///     A rigorous switch to categorize the (combat-focused) type of content that
    ///     the user is currently in; primarily using
    ///     <see cref="TerritoryIntendedUse" />.
    /// </summary>
    /// <param name="default">
    ///     The default content type to return if the switch doesn't resolve to anything.
    ///     <br />
    ///     Primarily here to make it easier in the future if this method is to get
    ///     more rigorous in regard to what returns as
    ///     <see cref="ContentType.OverWorld" />.
    /// </param>
    /// <returns>The determined <see cref="ContentType" />.</returns>
    private static ContentType? DetermineContentType
        (ContentType @default = GameHelpers.ContentType.OverWorld)
    {
        return TerritoryIntendedUse switch
        {
            TerritoryIntendedUseEnum.Barracks or
                TerritoryIntendedUseEnum.Rival_Wings or
                TerritoryIntendedUseEnum.Crystalline_Conflict or
                TerritoryIntendedUseEnum.Frontline =>
                GameHelpers.ContentType.PVP,

            TerritoryIntendedUseEnum.Dungeon or
                TerritoryIntendedUseEnum.Treasure_Map_Duty =>
                GameHelpers.ContentType.Dungeon,

            TerritoryIntendedUseEnum.Deep_Dungeon =>
                GameHelpers.ContentType.DeepDungeon,

            TerritoryIntendedUseEnum.Variant_Dungeon =>
                GameHelpers.ContentType.Variant,

            TerritoryIntendedUseEnum.Criterion_Duty or
                TerritoryIntendedUseEnum.Criterion_Savage_Duty =>
                GameHelpers.ContentType.Criterion,

            TerritoryIntendedUseEnum.Trial =>
                GameHelpers.ContentType.Trial,

            TerritoryIntendedUseEnum.Large_Scale_Raid or
                TerritoryIntendedUseEnum.Large_Scale_Savage_Raid =>
                GameHelpers.ContentType.FieldRaid,

            // 台服(TC)注意:這三個英文字面在台服比對不到(ContentName 是以 client 語言
            // 讀出的 CFC/PlaceName),但**不需要補繁中**——7.20 EXD dump 實查後三個都是死碼:
            //  * Delubrum Reginae 是 TerritoryType 936/937,TerritoryIntendedUse 52/53,
            //    已被上面的 Large_Scale_Raid / Large_Scale_Savage_Raid 分支攔下,
            //    這一行永遠輪不到(全表只有 936/937 用 52/53)。
            //  * Castrum Lacus Litore(帝國湖岸堡攻城戰)與 The Dalriada(旗艦達爾里阿達號
            //    攻略戰)沒有自己的 TerritoryType/CFC,它們是南方博茲雅戰線(920)與
            //    扎杜諾爾高原(975)裡的 DynamicEvent(16 / 32),所以 ContentName 在裡面
            //    仍然是所屬野外區域的名字——這兩個比對在**任何語言**都恆為 false。
            // 真正還活著的只有 MapID 520~527(優雷卡豐水之地的兵武塔＝Baldesion Arsenal),
            // 而那是數值判定,與語言無關。保留原樣以免影響其他語言客戶端。
            _ when
                (ContentName?.Contains("Delubrum") ?? false) ||
                (ContentName?.Contains("Lacus") ?? false) ||
                (ContentName?.Contains("Dalriada") ?? false) ||
                MapID is >= 520 and <= 527 =>
                GameHelpers.ContentType.FieldRaid,

            TerritoryIntendedUseEnum.Eureka or
                TerritoryIntendedUseEnum.Bozja or
                TerritoryIntendedUseEnum.Occult_Crescent or
                TerritoryIntendedUseEnum.Diadem or
                TerritoryIntendedUseEnum.Diadem_2 or
                TerritoryIntendedUseEnum.Diadem_3 =>
                GameHelpers.ContentType.FieldOperations,

            TerritoryIntendedUseEnum.Alliance_Raid =>
                GameHelpers.ContentType.ARaid,

            TerritoryIntendedUseEnum.Raid or
                TerritoryIntendedUseEnum.Raid_2 =>
                GameHelpers.ContentType.Raid,

            _ => @default,
        };
    }

    /// <summary>
    ///     A rigorous switch to categorize the difficulty of the content the user is
    ///     currently in; primarily using <see cref="ContentFinderConditionRow" />.
    /// </summary>
    /// <param name="default">
    ///     The default content difficulty to return if the switch doesn't resolve to
    ///     anything.<br />
    ///     Primarily here to make it easier in the future if this method is to get
    ///     more rigorous in regard to what returns as
    ///     <see cref="ContentDifficulty.Normal" />.
    /// </param>
    /// <returns>The determined <see cref="ContentDifficulty" />.</returns>
    private static ContentDifficulty? DetermineContentDifficulty
        (ContentDifficulty @default = GameHelpers.ContentDifficulty.Normal)
    {
        return ContentFinderConditionRow switch
        {
            _ when ContentDifficultyFromNameResolved =>
                ContentDifficultyFromName switch
                {
                    "Hard" => GameHelpers.ContentDifficulty.Hard,
                    "Extreme" => GameHelpers.ContentDifficulty.Extreme,
                    "Savage" => GameHelpers.ContentDifficulty.Savage,
                    _ => @default,
                },

            { ContentType.RowId: 1 } =>
                GameHelpers.ContentDifficulty.Normal,

            { ContentType.RowId: 2 } when
                ContentDifficultyFromName == "Hard" =>
                GameHelpers.ContentDifficulty.Hard,
            { ContentType.RowId: 4, HighEndDuty: false } when
                ContentDifficultyFromName == "Hard" =>
                GameHelpers.ContentDifficulty.Hard,

            { ContentType.RowId: 29 } when ContentDifficultyFromName == "Savage" =>
                GameHelpers.ContentDifficulty.FieldRaidsSavage,

            // 台服(TC)注意:Contains("Minstrel") 在台服比對不到,但**不需要補繁中**——
            // 台服的吟遊詩人敘事詩系列不是統一前綴,而是分散在「極 」前綴(極 神龍/極 月讀/
            // 極 黑迪斯/極 佐狄亞克/極 海德林/極 尼德霍格/極 永恆女王…)與
            // 「究極幻想」「蒼天幻想」「終極之戰」三個特例,這些全部已由
            // ContentDifficultyFromNameTC 判成 "Extreme",左邊的條件就會成立。
            { ContentType.RowId: 4 } when
                ContentDifficultyFromName == "Extreme" ||
                (ContentName?.Contains("Minstrel") ?? false) =>
                GameHelpers.ContentDifficulty.Extreme,

            { ContentType.RowId: 4, HighEndDuty: true } =>
                GameHelpers.ContentDifficulty.Unreal,

            { ContentType.RowId: 37 } =>
                GameHelpers.ContentDifficulty.Chaotic,

            { ContentType.RowId: 30, AllowUndersized: true } =>
                GameHelpers.ContentDifficulty.Criterion,

            { ContentType.RowId: 5 } when ContentDifficultyFromName == "Savage" =>
                GameHelpers.ContentDifficulty.Savage,

            { ContentType.RowId: 30, AllowUndersized: false } =>
                GameHelpers.ContentDifficulty.CriterionSavage,

            { ContentType.RowId: 28 } =>
                GameHelpers.ContentDifficulty.Ultimate,

            _ => @default,
        };
    }
}
