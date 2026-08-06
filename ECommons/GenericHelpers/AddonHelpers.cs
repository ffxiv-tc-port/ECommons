using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace ECommons;
public static unsafe partial class GenericHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkUnitBase* Addon)
        => Addon->IsVisible && Addon->UldManager.LoadedState == AtkLoadState.Loaded && Addon->IsFullyLoaded();

    public static bool IsReady(this AtkUnitBase Addon)
        => Addon.IsVisible && Addon.UldManager.LoadedState == AtkLoadState.Loaded && Addon.IsFullyLoaded();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkComponentNode* Addon)
        => Addon->AtkResNode.IsVisible() && Addon->Component->UldManager.LoadedState == AtkLoadState.Loaded;

    /// <summary>
    /// Null-safe replacement for reading <c>IsEnabled</c> on a button-derived component.
    /// <b>Always prefer this over dereferencing <c>IsEnabled</c> directly.</b>
    /// </summary>
    /// <remarks>
    /// FFXIVClientStructs resolves <c>AtkComponentButton.IsEnabled</c> as
    /// <c>OwnerNode-&gt;AtkResNode.NodeFlags</c> and performs no null check on <c>OwnerNode</c>.
    /// A component that has not finished setup - or whose owner node has already been torn down - has a null
    /// <c>OwnerNode</c>, so reading the property directly raises an AccessViolationException.
    /// AVE is a corrupted-state exception: neither <c>try</c>/<c>catch</c> nor any exception-isolation wrapper
    /// can recover from it, so the pointers must be validated before the property is read.
    /// </remarks>
    /// <returns><see langword="false"/> - treat as not clickable - when any pointer on the path is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponentEnabled(AtkComponentButton* button)
        => button != null && button->OwnerNode != null && button->IsEnabled;

    /// <inheritdoc cref="IsComponentEnabled(AtkComponentButton*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponentEnabled(AtkComponentRadioButton* radioButton)
        => radioButton != null && radioButton->OwnerNode != null && radioButton->IsEnabled;

    /// <inheritdoc cref="IsComponentEnabled(AtkComponentButton*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponentEnabled(AtkComponentCheckBox* checkBox)
        => checkBox != null && checkBox->OwnerNode != null && checkBox->IsEnabled;

    /// <inheritdoc cref="IsComponentEnabled(AtkComponentButton*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponentEnabled(AtkComponentListItemRenderer* listItem)
        => listItem != null && listItem->OwnerNode != null && listItem->IsEnabled;

    /// <summary>
    /// Null-safe replacement for <c>component-&gt;AtkResNode-&gt;IsVisible()</c>.
    /// <c>AtkComponentBase.AtkResNode</c> is a pointer field and is null before the component is set up,
    /// so dereferencing it unguarded is the same class of AccessViolationException as
    /// <see cref="IsComponentEnabled(AtkComponentButton*)"/>.
    /// </summary>
    /// <returns><see langword="false"/> when the component or its res node is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponentVisible(AtkComponentBase* component)
        => component != null && component->AtkResNode != null && component->AtkResNode->IsVisible();

    /// <summary>
    /// Gets a node given a chain of node IDs
    /// </summary>
    /// <param name="node">Root node of the addon</param>
    /// <param name="ids">Node IDs (starting from root) to the desired node</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params int[] ids)
    {
        if(node == null || ids.Length <= 0)
            return null;

        if(node->NodeId == ids[0])
        {
            if(ids.Length == 1)
                return node;

            var newList = new List<int>(ids);
            newList.RemoveAt(0);

            var childNode = node->ChildNode;
            if(childNode != null)
                return GetNodeByIDChain(childNode, [.. newList]);

            if((int)node->Type >= 1000)
            {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;
                childNode = uldManager.NodeList[0];
                return childNode == null ? null : GetNodeByIDChain(childNode, [.. newList]);
            }

            return null;
        }

        //check siblings
        var sibNode = node->PrevSiblingNode;
        return sibNode != null ? GetNodeByIDChain(sibNode, ids) : null;
    }

    /// <summary>
    /// Recursively gets the root node of an addon
    /// </summary>
    /// <param name="node">Starting node to search from</param>
    /// <returns></returns>
    public static unsafe AtkResNode* GetRootNode(AtkResNode* node)
    {
        var parent = node->ParentNode;
        return parent == null ? node : GetRootNode(parent);
    }

    /// <summary>
    /// Attempts to find out whether SelectString entry is enabled based on text color. 
    /// </summary>
    /// <param name="textNodePtr"></param>
    /// <returns></returns>
    [Obsolete("Incompatible with UI mods, use other methods")]
    public static bool IsSelectItemEnabled(AtkTextNode* textNodePtr)
    {
        var col = textNodePtr->TextColor;
        //EEE1C5FF
        return (col.A == 0xFF && col.R == 0xEE && col.G == 0xE1 && col.B == 0xC5)
            //7D523BFF
            || (col.A == 0xFF && col.R == 0x7D && col.G == 0x52 && col.B == 0x3B)
            || (col.A == 0xFF && col.R == 0xFF && col.G == 0xFF && col.B == 0xFF)
            // EEE1C5FF
            || (col.A == 0xFF && col.R == 0xEE && col.G == 0xE1 && col.B == 0xC5);
    }

    /// <summary>
    /// Returns <see langword="true"/> if screen isn't faded. 
    /// </summary>
    /// <returns></returns>
    public static bool IsScreenReady()
    {
        { if(TryGetAddonByName<AtkUnitBase>("NowLoading", out var addon) && addon->IsVisible) return false; }
        { if(TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var addon) && addon->IsVisible) return false; }
        { if(TryGetAddonByName<AtkUnitBase>("FadeBack", out var addon) && addon->IsVisible) return false; }
        return true;
    }

    /// <summary>
    /// Slower than <see cref="TryGetAddonByName"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="addon"></param>
    /// <param name="addonMaster"></param>
    /// <returns></returns>
    public static bool TryGetAddonMaster<T>(string addon, out T addonMaster) where T : IAddonMasterBase
    {
        if(TryGetAddonByName<AtkUnitBase>(addon, out var ptr))
        {
            addonMaster = (T)Activator.CreateInstance(typeof(T), (nint)ptr);
            return true;
        }
        addonMaster = default;
        return false;
    }

    public static bool TryGetAddonMaster<T>(out T addonMaster) where T : IAddonMasterBase
    {
        if(TryGetAddonByName<AtkUnitBase>(typeof(T).Name.Split(".")[^1], out var ptr))
        {
            addonMaster = (T)Activator.CreateInstance(typeof(T), (nint)ptr);
            return true;
        }
        addonMaster = default;
        return false;
    }

    /// <summary>
    /// Attempts to get first instance of addon by name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Addon"></param>
    /// <param name="AddonPtr"></param>
    /// <returns></returns>
    public static bool TryGetAddonByName<T>(string Addon, out T* AddonPtr) where T : unmanaged
    {
        var a = Svc.GameGui.GetAddonByName(Addon, 1);
        if(a == IntPtr.Zero)
        {
            AddonPtr = null;
            return false;
        }
        else
        {
            AddonPtr = (T*)a.Address;
            return true;
        }
    }

    // 移植自上游 NightmareXIV/ECommons 2d8d2f4(AtkValue 型別檢查輔助)。
    public static bool IsString(this ValueType type)
    {
        return type == ValueType.String || type == ValueType.String8 || type == ValueType.WideString || type == ValueType.ManagedString;
    }

    public static bool IsString(this AtkValue value)
    {
        var type = value.Type;
        if(type == ValueType.String || type == ValueType.String8 || type == ValueType.WideString || type == ValueType.ManagedString)
        {
            return value.String.HasValue;
        }
        return false;
    }
}
