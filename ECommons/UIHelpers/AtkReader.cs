using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace ECommons.UIHelpers;
#nullable disable

public abstract unsafe class AtkReader(AtkUnitBase* UnitBase, int BeginOffset = 0)
{
    public List<T> Loop<T>(int Offset, int Size, int MaxLength, bool IgnoreNull = false) where T : AtkReader
    {
        // 前 16 筆先放堆疊上的緩衝、結束時才建剛好大小的清單，省掉清單一路長大的重配；建立順序、例外點與內容不變。
        var buf = new LoopBuffer();
        var n = 0;
        List<T> ret = null;
        var fast = ExactCtor<T, nint, int>.Create != null;
        for(var i = 0; i < MaxLength; i++)
        {
            var r = fast
                ? ExactCtor<T, nint, int>.New((nint)UnitBase, Offset + (i * Size))
                : (AtkReader)Activator.CreateInstance(typeof(T), [(nint)UnitBase, Offset + (i * Size)]);
            if(r.IsNull && !IgnoreNull) break;
            if(ret != null)
            {
                ret.Add((T)r);
            }
            else if(n < LoopBuffer.Length)
            {
                buf[n++] = r;
            }
            else
            {
                ret = new List<T>(LoopBuffer.Length * 2);
                for(var j = 0; j < n; j++) ret.Add((T)buf[j]);
                ret.Add((T)r);
            }
        }
        if(ret == null)
        {
            ret = new List<T>(n);
            for(var j = 0; j < n; j++) ret.Add((T)buf[j]);
        }
        return ret;
    }

    [InlineArray(Length)]
    private struct LoopBuffer
    {
        public const int Length = 16;
        private AtkReader element;
    }

    public AtkReader(nint UnitBasePtr, int BeginOffset = 0) : this((AtkUnitBase*)UnitBasePtr, BeginOffset) { }

    public (nint UnitBase, int BeginOffset) AtkReaderParams => ((nint)UnitBase, BeginOffset);

    /// <remarks>
    /// 🔴 <c>UnitBase</c> 為 null 時解參考是 AccessViolation(corrupted-state exception,
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
        // 🔴 第三道守衛:型別是字串不代表指標非空。型別對而 String.Value 為 null 時,
        // MemoryHelper 會從位址 0 起掃 null 結尾 = AccessViolationException,而 AVE 是
        // corrupted-state exception,try/catch 攔不到。與上面 Type == 0 同樣回 null
        // (本檔的失敗語意一律是 null,呼叫端已經在處理可空)。
        if(value.String.Value == null) return null;
        return MemoryHelper.ReadSeStringNullTerminated((nint)value.String.Value);
    }

    protected string ReadString(int n)
    {
        var p = ReadStringPointer(n);
        return p == null ? null : MemoryHelper.ReadStringNullTerminated((nint)p);
    }

    /// <summary>同 <see cref="ReadString"/>（同樣的檢查與例外），但這一格的內容沒變時回 <paramref name="cache"/> 裡同一個字串實例。</summary>
    internal string ReadStringCached(int n, Utf8StringCache cache)
    {
        var p = ReadStringPointer(n);
        return p == null ? null : cache.Get(n + BeginOffset, p);
    }

    /// <summary>回 null＝這格沒有字串（型別 0 或指標為空）；型別不是字串時擲 <see cref="InvalidCastException"/>。</summary>
    private byte* ReadStringPointer(int n)
    {
        var num = n + BeginOffset;
        EnsureCount(UnitBase, num);
        var value = UnitBase->AtkValues[num];
        if(value.Type == 0)
        {
            return null;
        }
        // 與 EqualsAny(四個型別) 同義；不用 params 版本，那個每次呼叫配置一個陣列。
        if(value.Type is not (ValueType.String or ValueType.ManagedString or ValueType.String8 or ValueType.WideString)) throw new InvalidCastException($"Value {num} from Addon {GenericHelpers.Read(UnitBase->Name)} was requested as String but it was {value.Type}");
        // 🔴 第三道守衛,理由同 ReadSeString。
        return value.String.Value;
    }

    /// <remarks>
    /// 🔴 <paramref name="Addon"/> 為 null 時擲 <see cref="ArgumentNullException"/> 而不是解參考(那是攔不到的
    /// AccessViolationException)—— 與本函式既有的
    /// 「出界就擲例外」語意一致,且例外是<b>攔得到</b>的。
    /// </remarks>
    private void EnsureCount(AtkUnitBase* Addon, int num)
    {
        if(Addon == null) throw new ArgumentNullException(nameof(Addon), "AtkReader was constructed with a null AtkUnitBase.");
        if(Addon->AtkValues == null) throw new ArgumentOutOfRangeException(nameof(num), $"Addon {GenericHelpers.Read(Addon->Name)} has a null AtkValues array (AtkValuesCount={Addon->AtkValuesCount})");
        if(num >= Addon->AtkValuesCount) throw new ArgumentOutOfRangeException(nameof(num));
    }
}
