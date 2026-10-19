using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ECommons.UIHelpers;

/// <summary>
/// 依「哪一格」（呼叫端給的鍵）記住上次讀到的位元組與字串：內容逐位元組相同才回同一個字串實例，否則重新解碼並取代。
/// 解碼與 <c>MemoryHelper.ReadStringNullTerminated</c> 相同（<see cref="Encoding.UTF8"/>）。
/// 任何執行緒都可呼叫：槽位只放不可變的項目，競態最多多解碼一次；不用執行緒區域儲存。
/// </summary>
internal sealed unsafe class Utf8StringCache
{
    private const int MaxCachedBytes = 128;

    private sealed class Entry(byte[] bytes, string value)
    {
        public readonly byte[] Bytes = bytes;
        public readonly string Value = value;
    }

    private readonly Entry[] Slots;

    /// <param name="slotCount">2 的次方；鍵對它取餘數，撞槽只會多解碼、不會讀錯。</param>
    public Utf8StringCache(int slotCount)
    {
        if(slotCount <= 0 || (slotCount & (slotCount - 1)) != 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
        Slots = new Entry[slotCount];
    }

    public string Get(int key, byte* p)
    {
        var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p);
        if(span.Length > MaxCachedBytes) return Encoding.UTF8.GetString(span);
        ref var slot = ref Slots[key & (Slots.Length - 1)];
        var e = Volatile.Read(ref slot);
        if(e != null && span.SequenceEqual(e.Bytes)) return e.Value;
        var s = Encoding.UTF8.GetString(span);
        Volatile.Write(ref slot, new Entry(span.ToArray(), s));
        return s;
    }
}
