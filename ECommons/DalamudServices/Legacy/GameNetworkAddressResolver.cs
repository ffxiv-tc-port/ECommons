using Dalamud.Game;
using System;

namespace ECommons.DalamudServices.Legacy;

/// <summary>
/// The address resolver for the <see cref="GameNetwork"/> class.
/// </summary>
internal sealed class GameNetworkAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the ProcessZonePacketUp method.
    /// </summary>
    public IntPtr ProcessZonePacketUp { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        // 對不上時留 0，由 GameNetwork 判斷後不建 hook。
        if(sig.TryScanText("48 89 5C 24 ?? 48 89 74 24 ?? 4C 89 64 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 70", out var up)) this.ProcessZonePacketUp = up; // unnamed in cs
    }
}