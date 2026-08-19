using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace ECommons.UIHelpers;
#nullable disable

public abstract unsafe class AtkReader(AtkUnitBase* UnitBase, int BeginOffset = 0)
{
    public List<T> Loop<T>(int Offset, int Size, int MaxLength, bool IgnoreNull = false) where T : AtkReader
    {
        var ret = new List<T>();
        for(var i = 0; i < MaxLength; i++)
        {
            var r = (AtkReader)Activator.CreateInstance(typeof(T), [(nint)UnitBase, Offset + (i * Size)]);
            if(r.IsNull && !IgnoreNull) break;
            ret.Add((T)r);
        }
        return ret;
    }

    public AtkReader(nint UnitBasePtr, int BeginOffset = 0) : this((AtkUnitBase*)UnitBasePtr, BeginOffset) { }

    public (nint UnitBase, int BeginOffset) AtkReaderParams => ((nint)UnitBase, BeginOffset);

    /// <remarks>
    /// 🔴 <c>UnitBase</c> 為 null 時原本在第一行就 AccessViolation(corrupted-state exception,
    /// <c>try</c>/<c>catch</c> 攔不到)。讀不到 addon 就是「這個 reader 沒有資料」⇒ 回
    /// <see langword="true"/>,這也正好讓 <see cref="Loop{T}"/> 停在正確的地方。
    /// </remarks>
    public bool IsNull
    {
        get
        {
            if(UnitBase == null) return true;
            if(UnitBase->AtkValuesCount == 0) return true;
            var num = 0 + BeginOffset;
            EnsureCount(UnitBase, num);
            if(UnitBase->AtkValues[num].Type == 0) return true;
            return false;
        }
    }
    protected uint? ReadUInt(int n)
    {
        var num = n + BeginOffset;
        EnsureCount(UnitBase, num);
        var value = UnitBase->AtkValues[num];
        if(value.Type == 0)
        {
            return null;
        }
        if(value.Type != ValueType.UInt) throw new InvalidCastException($"Value {num} from Addon {GenericHelpers.Read(UnitBase->Name)} was requested as uint but it was {value.Type}");
        return value.UInt;
    }

    protected int? ReadInt(int n)
    {
        var num = n + BeginOffset;
        EnsureCount(UnitBase, num);
        var value = UnitBase->AtkValues[num];
        if(value.Type == 0)
        {
            return null;
        }
        if(value.Type != ValueType.Int) throw new InvalidCastException($"Value {num} from Addon {GenericHelpers.Read(
            UnitBase->Name)} was requested as int but it was {value.Type}");
        return value.Int;
    }

    protected bool? ReadBool(int n)
    {
        var num = n + BeginOffset;
        EnsureCount(UnitBase, num);
        var value = UnitBase->AtkValues[num];
        if(value.Type == 0)
        {
            return null;
        }
        if(value.Type != ValueType.Bool) throw new InvalidCastException($"Value {num} from Addon {GenericHelpers.Read(UnitBase->Name)} was requested as bool but it was {value.Type}");
        return value.Byte != 0;
    }

    protected SeString ReadSeString(int n)
    {
        var num = n + BeginOffset;
        EnsureCount(UnitBase, num);
        var value = UnitBase->AtkValues[num];
        if(value.Type == 0)
        {
            return null;
        }
        if(!value.Type.EqualsAny(ValueType.String, ValueType.String8, ValueType.WideString, ValueType.ManagedString)) throw new InvalidCastException($"Value {num} from Addon {GenericHelpers.Read(UnitBase->Name)} was requested as SeString but it was {value.Type}");
        // 🔴 第三道守衛:型別是字串<b>不代表</b>指標非空。型別對而 String.Value 為 null 時,
        // MemoryHelper 會從位址 0 起掃 null 結尾 = AccessViolationException,而 AVE 是
        // corrupted-state exception,try/catch 攔不到。與上面 Type == 0 同樣回 null
        // (本檔的失敗語意一律是 null,呼叫端已經在處理可空)。
        if(value.String.Value == null) return null;
        return MemoryHelper.ReadSeStringNullTerminated((nint)value.String.Value);
    }

    protected string ReadString(int n)
    {
        var num = n + BeginOffset;
        EnsureCount(UnitBase, num);
        var value = UnitBase->AtkValues[num];
        if(value.Type == 0)
        {
            return null;
        }
        if(!value.Type.EqualsAny(ValueType.String, ValueType.ManagedString, ValueType.String8, ValueType.WideString)) throw new InvalidCastException($"Value {num} from Addon {GenericHelpers.Read(UnitBase->Name)} was requested as String but it was {value.Type}");
        // 🔴 第三道守衛,理由同 ReadSeString。
        if(value.String.Value == null) return null;
        return MemoryHelper.ReadStringNullTerminated((nint)value.String.Value);
    }

    /// <remarks>
    /// 🔴 加上 <paramref name="Addon"/> 判空:原本第一行就對 null 解參考 = AccessViolationException
    /// (攔不到)。這裡改成擲 <see cref="ArgumentNullException"/> —— 與本函式既有的
    /// 「出界就擲例外」語意一致,且例外是<b>攔得到</b>的。
    /// </remarks>
    private void EnsureCount(AtkUnitBase* Addon, int num)
    {
        if(Addon == null) throw new ArgumentNullException(nameof(Addon), "AtkReader was constructed with a null AtkUnitBase.");
        if(Addon->AtkValues == null) throw new ArgumentOutOfRangeException(nameof(num), $"Addon {GenericHelpers.Read(Addon->Name)} has a null AtkValues array (AtkValuesCount={Addon->AtkValuesCount})");
        if(num >= Addon->AtkValuesCount) throw new ArgumentOutOfRangeException(nameof(num));
    }
}
