using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ECommons.Automation.UIInput;
public unsafe class ClickHelper
{
    private static InvokeListener? ListenerInternal;
    public delegate nint InvokeListener(nint a1, AtkEventType a2, uint a3, AtkEvent* a4);
    public static InvokeListener Listener
    {
        get
        {
            ListenerInternal ??= EzDelegate.Get<InvokeListener>("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 0F B7 FA");
            return ListenerInternal;
        }
    }

    public unsafe delegate IntPtr ReceiveEventDelegate(AtkEventListener* eventListener, EventType evt, uint which, void* eventData, void* inputData);

    public static ReceiveEventDelegate GetReceiveEvent(AtkEventListener* listener)
    {
        var receiveEventAddress = new IntPtr(listener->VirtualTable->ReceiveEvent);
        return Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(receiveEventAddress)!;
    }

    public static void InvokeReceiveEvent(AtkEventListener* eventListener, EventType type, uint which, EventData eventData, InputData inputData)
    {
        var receiveEvent = GetReceiveEvent(eventListener);
        receiveEvent(eventListener, type, which, eventData.Data, inputData.Data);
    }

    public static void ClickAddonComponent(AtkComponentBase* unitbase, AtkComponentNode* target, uint which, EventType type, EventData? eventData = null, InputData? inputData = null)
    {
        EventData? newEventData = null;
        InputData? newInputData = null;
        if(eventData == null)
        {
            newEventData = EventData.ForNormalTarget(target, unitbase);
        }
        if(inputData == null)
        {
            newInputData = InputData.Empty();
        }

        InvokeReceiveEvent(&unitbase->AtkEventListener, type, which, eventData ?? newEventData!, inputData ?? newInputData!);
        newEventData?.Dispose();
        newInputData?.Dispose();
    }

    /// <summary>
    /// Invoke a click by name. Intended for calling clicks via user-entered strings.
    /// </summary>
    /// <param name="clickName">Name of the click, which is ClassName_MethodName.</param>
    /// <param name="addon">Addon instance.</param>
    public static void SendClick(string clickName, nint addon = default)
    {
        var classAndMethodNames = GetAvailableClicks();

        foreach(var classAndMethod in classAndMethodNames)
        {
            if(classAndMethod.Equals(clickName))
            {
                var className = clickName[..clickName.IndexOf('_')];
                var type = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name == className);
                if(type != null)
                {
                    var constructor = type.GetConstructor([typeof(nint)]);
                    if(constructor != null)
                    {
                        var instance = constructor.Invoke([addon]);
                        var methodName = clickName[(className.Length + 1)..];
                        var method = type.GetMethod(methodName);

                        if(method != null)
                        {
                            method.Invoke(instance, null);
                            return;
                        }
                        else
                            Svc.Log.Debug($"Method {methodName} not found for {clickName}");
                    }
                }
                Svc.Log.Debug($"Click class {className} not found.");
            }
        }
    }


    /// <summary>
    /// Return all the click names that can be passed to <see cref="SendClick(string, nint)"/>.
    /// </summary>
    public static string[] GetAvailableClicks()
    {
        var classesAndMethods = Assembly.GetExecutingAssembly().GetTypes()
        .Where(type => type.FullName!.StartsWith($"{typeof(AddonMaster).FullName}+") && type.DeclaringType == typeof(AddonMaster))
        .SelectMany(type => type.GetMethods()
            .Where(m => m.DeclaringType != typeof(object) && !m.IsSpecialName)
            .Select(method => $"{type.Name}_{method.Name}"));

        return classesAndMethods.ToArray();
    }
}

// Every method below dereferences native pointers that the game leaves null while a component is being set up
// or torn down: the component's OwnerNode, the addon it belongs to, and the AtkEvent taken from its event
// manager. An AccessViolationException raised here is a corrupted-state exception that no try/catch can
// recover from, so each pointer is checked and a missing one makes the click a no-op instead of a crash.
public static unsafe class ClickHelperExtensions
{
    public static void ClickAddonButton(this AtkComponentButton target, AtkComponentBase* addon, uint which, EventType type = EventType.CHANGE, EventData? eventData = null)
    {
        var ownerNode = target.AtkComponentBase.OwnerNode;
        if(addon == null || ownerNode == null) return;
        ClickHelper.ClickAddonComponent(addon, ownerNode, which, type, eventData);
    }

    public static void ClickRadioButton(this AtkComponentRadioButton target, AtkComponentBase* addon, uint which, EventType type = EventType.CHANGE)
    {
        var ownerNode = target.OwnerNode;
        if(addon == null || ownerNode == null) return;
        ClickHelper.ClickAddonComponent(addon, ownerNode, which, type);
    }

    public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon, AtkEvent* eventData)
    {
        if(addon == null || eventData == null) return;
        ClickHelper.Listener.Invoke((nint)addon, eventData->State.EventType, eventData->Param, eventData);
    }

    public static void ClickAddonButton(this AtkCollisionNode target, AtkUnitBase* addon, AtkEvent* eventData)
    {
        if(addon == null || eventData == null) return;
        ClickHelper.Listener.Invoke((nint)addon, eventData->State.EventType, eventData->Param, eventData);
    }

    public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon)
    {
        var ownerNode = target.AtkComponentBase.OwnerNode;
        if(addon == null || ownerNode == null) return;
        var btnRes = ownerNode->AtkResNode;
        var evt = (AtkEvent*)btnRes.AtkEventManager.Event;
        if(evt == null) return;

        addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, btnRes.AtkEventManager.Event);
    }

    public static void ClickAddonButton(this AtkCollisionNode target, AtkUnitBase* addon)
    {
        if(addon == null) return;
        var btnRes = target.AtkResNode;
        var evt = (AtkEvent*)btnRes.AtkEventManager.Event;

        // The list is not guaranteed to contain a MouseClick event; walking past its end used to crash.
        while(evt != null && evt->State.EventType != AtkEventType.MouseClick)
            evt = evt->NextEvent;
        if(evt == null) return;

        addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, btnRes.AtkEventManager.Event);
    }


    public static void ClickRadioButton(this AtkComponentRadioButton target, AtkUnitBase* addon)
    {
        var ownerNode = target.OwnerNode;
        if(addon == null || ownerNode == null) return;
        var btnRes = ownerNode->AtkResNode;
        var evt = (AtkEvent*)btnRes.AtkEventManager.Event;
        if(evt == null) return;

        Svc.Log.Debug($"{evt->State.EventType} {evt->Param}");
        addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, btnRes.AtkEventManager.Event);
    }

    public static void ClickCheckBox(this AtkComponentCheckBox target, AtkUnitBase* addon)
    {
        //var btnRes = target.AtkComponentButton.AtkComponentBase.OwnerNode->AtkResNode;
        var ownerNode = target.OwnerNode;
        if(addon == null || ownerNode == null) return;
        var btnRes = ownerNode->AtkResNode;
        var evt = (AtkEvent*)btnRes.AtkEventManager.Event;
        if(evt == null) return;
        var data = stackalloc AtkEventData[1];
        addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, evt, data); // btnRes.AtkEventManager.Event);
    }
}
