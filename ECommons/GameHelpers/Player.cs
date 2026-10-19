using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using Aetheryte = Lumina.Excel.Sheets.Aetheryte;
using GrandCompany = ECommons.ExcelServices.GrandCompany;
#nullable disable

namespace ECommons.GameHelpers;

/// <summary>
/// In general, these properties and methods should be made in a way that does not throws <see cref="NullReferenceException"/>, where feasible.
/// </summary>
/// <remarks>
/// 遊戲位址沒解出(改版後特徵碼對不上)時不擲例外,回各成員的不可用值:「可以嗎／有嗎」類回 <see langword="false"/> 或 0,
/// 「受限／在忙嗎」類(<see cref="IsInDuty"/>、<see cref="IsInPvP"/>、<see cref="IsMoving"/>、<see cref="IsAnimationLocked"/>)回 <see langword="true"/>,
/// <see cref="AnimationLock"/> 回 <see cref="float.MaxValue"/>。位址由 <see cref="CSAddress"/> 判,每個只記一次 log。
/// </remarks>
public static unsafe class Player
{
    public static readonly Number MaxLevel = 100;
    public static IPlayerCharacter Object => Svc.Objects.LocalPlayer;
    public static bool Available => Svc.Objects.LocalPlayer != null;
    public static bool AvailableThreadSafe => CSAddress.TryGameObjectManager(out var manager) && manager->Objects.IndexSorted[0].Value != null;
    public static bool Interactable => Available && Object.IsTargetable;
    public static bool IsBusy => GenericHelpers.IsOccupied() || Object.IsCasting || IsMoving || IsAnimationLocked || Svc.Condition[ConditionFlag.InCombat];
    public static ulong CID => Svc.PlayerState.ContentId;
    public static StatusList Status => Svc.Objects.LocalPlayer?.StatusList;
    public static string Name => Svc.Objects.LocalPlayer?.Name.ToString();
    public static string NameWithWorld => GetNameWithWorld(Svc.Objects.LocalPlayer);
    public static string GetNameWithWorld(this IPlayerCharacter pc) => pc == null ? null : (pc.Name.ToString() + "@" + pc.HomeWorld.ValueNullable?.Name.ToString());

    public static int Level => Svc.Objects.LocalPlayer?.Level ?? 0;
    public static bool IsLevelSynced => CSAddress.TryPlayerState(out var state) && state->IsLevelSynced;
    public static int SyncedLevel => CSAddress.TryPlayerState(out var state) ? state->SyncedLevel : 0;
    public static int UnsyncedLevel => GetUnsyncedLevel(GetJob(Object));
    /// <summary>
    /// 取得指定職業不受等級同步影響的實際等級。查不到時回 0。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>GetRowOrDefault</c> 回的是 <c>ClassJob?</c>,對它直接取 <c>.Value</c> 在缺列時照樣擲例外;
    /// <see cref="Job"/> 可以從任意 uint 轉型過來(遊戲新增職業而列舉還沒補時就落在表外)。
    /// <para>
    /// ⚠️ <c>ExpArrayIndex</c> 是 <c>sbyte</c>,而<b>第 0 列(冒險者)是 -1</b>,拿去索引 <c>ClassJobLevels</c> 會擲
    /// <see cref="IndexOutOfRangeException"/>;沒有玩家時 <see cref="GetJob"/> 就是回 <c>Job.ADV</c>(0),這條路是常態不是例外。
    /// 兩種情況都回 0,與 <see cref="Level"/> 在沒有玩家時的回值一致;只有「缺列」會記 log。
    /// </para>
    /// </remarks>
    public static int GetUnsyncedLevel(Job job)
    {
        var classJob = job.GetDataOrDefault();
        if(classJob == null)
        {
            LogMissingRowOnce(nameof(ClassJob), (uint)job, $"{nameof(GetUnsyncedLevel)} 回 0");
            return 0;
        }

        var expArrayIndex = classJob.Value.ExpArrayIndex;
        if(expArrayIndex < 0) return 0;

        return CSAddress.TryPlayerState(out var state) ? state->ClassJobLevels[expArrayIndex] : 0;
    }

    public static bool IsInHomeWorld => !Player.Available ? false : Svc.Objects.LocalPlayer.HomeWorld.RowId == Svc.Objects.LocalPlayer.CurrentWorld.RowId;
    public static bool IsInHomeDC => !Player.Available ? false : Svc.Objects.LocalPlayer.CurrentWorld.Value.DataCenter.RowId == Svc.Objects.LocalPlayer.HomeWorld.Value.DataCenter.RowId;
    public static string HomeWorld => Svc.Objects.LocalPlayer?.HomeWorld.Value.Name.ToString();
    public static string CurrentWorld => Svc.Objects.LocalPlayer?.CurrentWorld.Value.Name.ToString();
    public static string HomeDataCenter => Svc.Data.GetExcelSheet<World>().GetRowOrDefault(HomeWorldId)?.DataCenter.ValueNullable?.Name.ToString();
    public static string CurrentDataCenter => Svc.Data.GetExcelSheet<World>().GetRowOrDefault(CurrentWorldId)?.DataCenter.ValueNullable?.Name.ToString();

    public static Character* Character => (Character*)Svc.Objects.LocalPlayer.Address;
    public static BattleChara* BattleChara => (BattleChara*)Svc.Objects.LocalPlayer.Address;
    public static GameObject* GameObject => (GameObject*)Svc.Objects.LocalPlayer.Address;

    public static uint Territory => Svc.ClientState.TerritoryType;
    public static TerritoryIntendedUseEnum TerritoryIntendedUse => (TerritoryIntendedUseEnum)(Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Territory)?.TerritoryIntendedUse.ValueNullable?.RowId ?? default);
    /// <summary>
    /// 家傳送點所在的區域 id。查不到傳送點時回 0。
    /// </summary>
    /// <remarks>
    /// 🔴 同 <see cref="GetUnsyncedLevel"/>:<c>GetRowOrDefault(...).Value</c> 在缺列時擲例外,要明確判空。
    /// <c>HomeAetheryteId</c> 是遊戲記憶體裡的即時值,可能領先本地 sheet(新版本加了傳送點)。
    /// 📌 退路 0 與 Aetheryte 第 0 列的 <c>Territory</c> 同值,呼叫端不會多出一種要處理的狀態。
    /// </remarks>
    public static uint HomeAetheryteTerritory
    {
        get
        {
            if(!CSAddress.TryPlayerState(out var state)) return 0;
            var id = state->HomeAetheryteId;
            var aetheryte = Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(id);
            if(aetheryte == null)
            {
                LogMissingRowOnce(nameof(Aetheryte), id, $"{nameof(HomeAetheryteTerritory)} 回 0");
                return 0;
            }

            return aetheryte.Value.Territory.RowId;
        }
    }
    public static bool IsInDuty => !CSAddress.TryGameMain(out var gameMain) || gameMain->CurrentContentFinderConditionId != 0;
    /// <remarks>
    /// 🔴 <c>MJIManager.Instance()</c> 真的可能回 <see langword="null"/>:它宣告成
    /// <c>[StaticAddress(..., isPointer: true)]</c>,回的是靜態位址裡存放的指標值,產生器只在
    /// <b>特徵碼失配</b>時擲例外、對回傳值不判空。
    /// 玩家沒進過無人島時遊戲還沒配置這個管理器,那個指標欄位就是 null(常態,不是異常),
    /// 裸解參考是 AccessViolation,而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
    /// 取不到管理器時回 <see langword="false"/>(＝不在島上,保守)。
    /// </remarks>
    public static bool IsOnIsland
    {
        get
        {
            var mji = CSAddress.MJIManagerOrNull();
            if(mji == null) return false;
            return mji->IsPlayerInSanctuary;
        }
    }
    public static bool IsInPvP => !CSAddress.Resolved(GameMain.MemberFunctionPointers.IsInPvPInstance) || GameMain.IsInPvPInstance();

    public static Job Job => GetJob(Svc.Objects.LocalPlayer);
    public static GrandCompany GrandCompany => CSAddress.TryPlayerState(out var state) ? (GrandCompany)state->GrandCompany : GrandCompany.Unemployed;
    public static Job GetJob(this IPlayerCharacter pc) => (Job)(pc?.ClassJob.RowId ?? 0);

    public static uint HomeWorldId => Player.Object?.HomeWorld.RowId ?? 0;
    public static uint CurrentWorldId => Player.Object?.CurrentWorld.RowId ?? 0;
    public static uint JobId => Player.Object?.ClassJob.RowId ?? 0;
    public static uint OnlineStatus => Player.Object?.OnlineStatus.RowId ?? 0;

    public static Vector3 Position => Available ? Object.Position : Vector3.Zero;
    public static float Rotation => Available ? Object.Rotation : 0;
    /// <remarks>
    /// 🔴 <c>AgentMap.Instance()</c> 真的可能回 <see langword="null"/>:它是 <c>[Agent]</c>
    /// 來源產生器產出的取得器,產生的碼是 <c>AgentModule.Instance()</c> 為 null 時直接回 null,
    /// 否則走 <c>GetAgentByInternalId</c>(那也不保證非 null)。
    /// 取不到 agent 時退回只看 <see cref="IsJumping"/>,保留原式 <c>IsPlayerMoving || IsJumping</c>
    /// 的另一半語意(＝不會因為讀不到 agent 就把「跳躍中」誤報成靜止)。
    /// </remarks>
    public static bool IsMoving
    {
        get
        {
            if(!Available) return false;
            if(!CSAddress.AgentChainResolved()) return true;
            var agent = AgentMap.Instance();
            if(agent != null && agent->IsPlayerMoving) return true;
            return IsJumping;
        }
    }
    /// <remarks><c>Character.IsJumping</c> 的位址沒解出時只看兩個條件旗標(＝<see cref="Jumping"/>),不是一律回 true:旗標那一半仍然是真值。</remarks>
    public static bool IsJumping => Available && (Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61]
        || (CSAddress.Resolved(FFXIVClientStructs.FFXIV.Client.Game.Character.Character.MemberFunctionPointers.IsJumping) && Character->IsJumping()));
    public static bool Mounted => Svc.Condition[ConditionFlag.Mounted];
    public static bool Mounting => Svc.Condition[ConditionFlag.MountOrOrnamentTransition];
    /// <summary>
    /// 目前區域是否允許騎乘,且玩家至少擁有一隻坐騎。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>GetRow</c> 對不存在的列<b>擲例外</b>不回 null。<see cref="Territory"/> 在讀取畫面/尚未進場時
    /// 可能是 0 或一個本地 sheet 沒有的區域,原寫法會讓這個看似無害的布林屬性擲例外
    /// —— 而本類別的契約(見類別註解)是「盡量不要擲例外」。
    /// 讀不到區域資料時回 <see langword="false"/>(＝保守,不會誤導呼叫端去召喚坐騎)。
    /// </remarks>
    public static bool CanMount => Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Territory)?.Mount == true && CSAddress.TryPlayerState(out var state) && state->NumOwnedMounts > 0;
    public static bool CanFly => CSAddress.Resolved(Control.MemberFunctionPointers.GetFlightAllowedStatus) && Control.CanFly;

    public static float AnimationLock => CSAddress.TryActionManager(out var manager) ? *(float*)((nint)manager + 8) : float.MaxValue;
    public static bool IsAnimationLocked => AnimationLock > 0;
    public static bool IsCasting => Available && Object.IsCasting();
    public static bool IsDead => Svc.Condition[ConditionFlag.Unconscious];
    /// <remarks>
    /// 🔴 <c>AgentRevive.Instance()</c> 真的可能回 <see langword="null"/>：它是 <c>[Agent]</c>
    /// 來源產生器產出的取得器 —— <c>AgentModule.Instance()</c> 為 null 時直接回 null，
    /// 否則走 <c>GetAgentByInternalId</c>（那也不保證非 null），
    /// 與 <c>[StaticAddress]</c> 那種「失配時擲例外、永不回 null」的取得器不同。
    /// 裸解參考在登入/登出交界是 AccessViolation，而 AVE 是 corrupted-state exception，
    /// <c>try</c>/<c>catch</c> 攔不到。取不到 agent 時回 <see langword="false"/>（＝保守）。
    /// </remarks>
    public static bool Revivable
    {
        get
        {
            if(!IsDead) return false;
            var agent = CSAddress.AgentChainResolved() ? AgentRevive.Instance() : null;
            return agent != null && agent->ReviveState != 0;
        }
    }

    public static float DistanceTo(Vector3 other) => Vector3.Distance(Position, other);
    public static float DistanceTo(Vector2 other) => Vector2.Distance(Position.ToVector2(), other);
    public static float DistanceTo(IGameObject other) => Vector3.Distance(Position, other.Position);

    [Obsolete("Use IsJumping")]
    public static unsafe bool Dismounting => **(byte**)(Svc.Objects.LocalPlayer.Address + 1400) == 1;
    [Obsolete("Use IsJumping")]
    public static bool Jumping => Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61];

    private static readonly HashSet<(string Sheet, uint Row)> ReportedMissingRows = [];

    /// <summary>
    /// 同一個(表,列)只記一次 —— 本類別的成員多半被每幀迴圈讀取,不設閘門會把 log 灌爆。
    /// </summary>
    /// <remarks>
    /// 用 <c>Information</c> 等級:這是要使用者回報的診斷。
    /// 🔴 刻意記 log 而不是靜默回預設值:缺列代表本地 sheet 與遊戲對不上,
    /// 靜默吞掉會把看得見的錯誤變成看不見的錯誤。
    /// </remarks>
    private static void LogMissingRowOnce(string sheet, uint row, string fallback)
    {
        lock(ReportedMissingRows)
        {
            if(!ReportedMissingRows.Add((sheet, row))) return;
        }
        PluginLog.Information($"[ECommons] {sheet} 表裡沒有第 {row} 列,{fallback}。(同一列只記這一次)");
    }
}
