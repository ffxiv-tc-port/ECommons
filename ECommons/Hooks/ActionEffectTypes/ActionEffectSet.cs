using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using Action = Lumina.Excel.Sheets.Action;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace ECommons.Hooks.ActionEffectTypes;

public readonly unsafe record struct ActionEffectSet
{
    public readonly Action? Action { get; }

    public readonly Item? Item { get; }

    public readonly EventItem? EventItem { get; }

    public readonly Mount? Mount { get; }

    public readonly ushort IconId { get; }

    public readonly string Name { get; }

    public readonly IGameObject? Target { get; }

    public readonly IGameObject? Source { get; }

    public readonly Character? SourceCharacter { get; }

    public readonly TargetEffect[] TargetEffects { get; }

    public readonly Vector3 Position { get; }

    public readonly EffectHeader Header { get; }

    public ActionEffectSet(uint sourceID, Character* sourceCharacter, Vector3* pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail)
    {
        switch(effectHeader->ActionType)
        {
            case ActionType.KeyItem:
                EventItem = Svc.Data.GetExcelSheet<EventItem>()!.GetRowOrDefault(effectHeader->ActionID);
                Name = CachedName(effectHeader->ActionType, effectHeader->ActionID, EventItem?.Singular);
                IconId = EventItem?.Icon ?? 0;
                break;

            case ActionType.Item:
                var id = effectHeader->ActionID > 1000000 ? effectHeader->ActionID - 1000000 : effectHeader->ActionID;
                Item = Svc.Data.GetExcelSheet<Item>()!.GetRowOrDefault(id);
                Name = CachedName(effectHeader->ActionType, id, Item?.Name);
                IconId = Item?.Icon ?? 0;
                break;

            case ActionType.Mount:
                Mount = Svc.Data.GetExcelSheet<Mount>()!.GetRowOrDefault(effectHeader->ActionID);
                Name = CachedName(effectHeader->ActionType, effectHeader->ActionID, Mount?.Singular);
                IconId = Mount?.Icon ?? 0;
                break;

            default:
                Action = Svc.Data.GetExcelSheet<Action>()!.GetRowOrDefault(effectHeader->ActionID);
                Name = CachedName(effectHeader->ActionType, effectHeader->ActionID, Action?.Name);

                var actionCate = Action?.ActionCategory.ValueNullable?.RowId ?? 0;

                IconId = actionCate == 1 ? (ushort)101 // Auto Attack
                    : effectHeader->ActionID == 3 ? (ushort)104 //Sprint
                    : effectHeader->ActionID == 4 ? (ushort)118 //Mount
                    : Action?.Icon ?? 0;
                break;
        }
        Target = Svc.Objects.SearchById(effectHeader->AnimationTargetId);
        Source = Svc.Objects.SearchById(sourceID);
        SourceCharacter = *sourceCharacter;
        Position = *pos;
        Header = *effectHeader;

        TargetEffects = new TargetEffect[effectHeader->TargetCount];
        for(var i = 0; i < effectHeader->TargetCount; i++)
        {
            TargetEffects[i] = new TargetEffect(effectTail[i], effectArray + 8 * i);
        }
    }

    // 名稱只由（種類, 列 ID）決定、遊戲表執行期間不變：每個技能效果都會建一次這個結構，名稱字串每種只轉一次。
    // 鍵含原始 ActionType（不是 switch 的分支），與舊版每次 ToString() 的結果逐字相同。
    private static readonly ConcurrentDictionary<ulong, string> NameCache = new();

    private static string CachedName(ActionType type, uint rowId, ReadOnlySeString? name)
    {
        var key = ((ulong)(byte)type << 32) | rowId;
        if(NameCache.TryGetValue(key, out var cached)) return cached;
        var value = name?.ToString() ?? string.Empty;
        NameCache.TryAdd(key, value);
        return value;
    }

    public Dictionary<ulong, uint> GetSpecificTypeEffect(ActionEffectType type)
    {
        var result = new Dictionary<ulong, uint>();
        foreach(var effect in TargetEffects)
        {
            if(effect.GetSpecificTypeEffect(type, out var e))
            {
                //Is this value or Damage? IDK about it.
                result[effect.TargetID] = e.value;
            }
        }
        return result;
    }

    public override string ToString()
    {
        var str = $"S:{Source?.Name}, T:{Target?.Name}, Lock:{Header.AnimationLockTime}";
        str += $"\nType: {Header.ActionType}, Name: {Action?.Name}({Action?.RowId})";
        if(TargetEffects != null)
        {
            foreach(var effect in TargetEffects)
            {
                str += "\n" + effect.ToString();
            }
        }
        return str;
    }
}
