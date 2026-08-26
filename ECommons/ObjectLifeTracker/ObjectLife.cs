using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.Logging;
using ECommons.Schedulers;
using System;
using System.Collections.Generic;

namespace ECommons.ObjectLifeTracker;
#nullable disable

public static class ObjectLife
{
    private delegate IntPtr IGameObject_ctor(IntPtr obj);

    private const string CtorSig = "48 8D 05 ?? ?? ?? ?? C7 81 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 01 48 8B C1 C3";

    private static Hook<IGameObject_ctor> IGameObject_ctor_hook = null;
    /// <summary>
    /// Delegate pointing at the original constructor address (no trampoline involved).
    /// <see cref="Dispose"/> sets the hook field back to null; by that point Disable() and Dispose()
    /// have already run and the original bytes are restored, so calling this delegate can not
    /// recurse back into the detour.
    /// This fallback is mandatory here: the hooked function is a constructor that installs the vtable
    /// and returns this. Skipping the original call would leave an object without a vtable, which
    /// crashes later - so "skip this invocation" is NOT an acceptable outcome for this hook.
    /// </summary>
    private static IGameObject_ctor OriginalCtor = null;
    private static Dictionary<IntPtr, long> IGameObjectLifeTime = null;
    public static Action<nint> OnObjectCreation = null;

    internal static void Init()
    {
        new TickScheduler(() =>
        {
            IGameObjectLifeTime = [];
#pragma warning disable CS0618 // Type or member is obsolete
            var ctorAddress = Svc.SigScanner.ScanText(CtorSig);
            OriginalCtor ??= EzDelegate.Get<IGameObject_ctor>(ctorAddress);
            IGameObject_ctor_hook = Svc.Hook.HookFromAddress<IGameObject_ctor>(ctorAddress, IGameObject_ctor_detour);
#pragma warning restore CS0618 // Type or member is obsolete
            IGameObject_ctor_hook.Enable();
            foreach(var x in Svc.Objects)
            {
                IGameObjectLifeTime[x.Address] = Environment.TickCount64;
            }
        });
    }

    internal static void Dispose()
    {
        if(IGameObject_ctor_hook != null)
        {
            IGameObject_ctor_hook.Disable();
            IGameObject_ctor_hook.Dispose();
            IGameObject_ctor_hook = null;
        }
        IGameObjectLifeTime = null;
    }

    private static IntPtr IGameObject_ctor_detour(IntPtr ptr)
    {
        // Dispose() sets both the hook field and the dictionary back to null while this detour may
        // still be executing. Snapshot both once and only use the locals afterwards.
        var hook = IGameObject_ctor_hook;
        var lifeTime = IGameObjectLifeTime;
        if(lifeTime != null)
        {
            lifeTime[ptr] = Environment.TickCount64;
        }
        else
        {
            // This used to throw. Throwing here unwinds straight back into native game code AND
            // skips the original constructor entirely. Drop the bookkeeping instead and keep going.
            PluginLog.Information($"IGameObjectLifeTime is null (ObjectLife module not initialised, or already disposed); skipping the life time record for {ptr:X16}.");
        }
        var original = hook?.OriginalDisposeSafe ?? OriginalCtor;
        if(original == null)
        {
            // Unreachable in practice: OriginalCtor is assigned before the hook is ever created.
            PluginLog.Information($"IGameObject constructor hook was disposed mid-call and no original delegate is available; the object at {ptr:X16} is left unconstructed.");
            return ptr;
        }
        var ret = original(ptr);

        if(OnObjectCreation != null)
        {
            try
            {
                OnObjectCreation(ptr);
            }
            catch(Exception e)
            {
                e.Log($"Exception in IGameObject_ctor_detour");
            }
        }
        return ret;
    }

    public static long GetLifeTime(this IGameObject o)
    {
        return Environment.TickCount64 - GetSpawnTime(o);
    }

    public static float GetLifeTimeSeconds(this IGameObject o)
    {
        return (float)o.GetLifeTime() / 1000f;
    }

    public static long GetSpawnTime(this IGameObject o)
    {
        // Snapshot: Dispose() nulls the hook and the dictionary together, so reading the field twice
        // can see it disappear between the guard and the lookup.
        var lifeTime = IGameObjectLifeTime;
        if(IGameObject_ctor_hook == null || lifeTime == null) throw new Exception("Object life tracker was not initialized");
        if(lifeTime.TryGetValue(o.Address, out var result))
        {
            return result;
        }
        else
        {
            PluginLog.Warning($"Warning: object life data could not be found\n" +
                $"Object addr: {o.Address:X16} ID: {o.EntityId:X8} Name: {o.Name}");
            return 0;
        }
    }
}
