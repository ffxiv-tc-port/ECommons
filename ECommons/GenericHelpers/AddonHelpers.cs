using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
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
    /// <remarks>
    /// 原本無條件解 <paramref name="Addon"/>,傳入 null 直接 AccessViolationException
    /// (corrupted-state exception,try/catch 攔不到)。
    /// </remarks>
    /// <returns><see langword="false"/> 當 <paramref name="Addon"/> 為 null ——「不存在」就是「沒 ready」。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkUnitBase* Addon)
        => Addon != null && Addon->IsVisible && Addon->UldManager.LoadedState == AtkLoadState.Loaded && Addon->IsFullyLoaded();

    /// <remarks>
    /// ⚠️ 這個多載<b>擋不了 null</b>,也無法擋:<see cref="AtkUnitBase"/> 是 struct,參數是<b>傳值</b>的,
    /// 複製動作發生在呼叫端(<c>ptr-&gt;IsReady()</c> 等同 <c>(*ptr).IsReady()</c>)。
    /// 指標為 null 時 AccessViolation 在<b>進入本函式之前</b>就已經發生,這裡再怎麼判空都來不及。
    /// 🔑 手上是指標就改用 <see cref="IsAddonReady(AtkUnitBase*)"/>,它有判空。
    /// </remarks>
    public static bool IsReady(this AtkUnitBase Addon)
        => Addon.IsVisible && Addon.UldManager.LoadedState == AtkLoadState.Loaded && Addon.IsFullyLoaded();

    /// <remarks>
    /// <c>AtkComponentNode.Component</c> 是指標欄位,元件尚未 setup 完成或已開始拆解時是 null,
    /// 無防護地讀 <c>Component-&gt;UldManager</c> 會觸發 AccessViolationException(無法被 try/catch 攔截)。
    /// </remarks>
    /// <returns>路徑上任一指標為 null 時回 <see langword="false"/>(視為尚未就緒)。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkComponentNode* Addon)
        => Addon != null && Addon->AtkResNode.IsVisible() && Addon->Component != null && Addon->Component->UldManager.LoadedState == AtkLoadState.Loaded;

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
                // GetAsAtkComponentNode 是原生 MemberFunction,型別不符時回 null。
                var componentNode = node->GetAsAtkComponentNode();
                if(componentNode == null)
                    return null;

                // 元件尚未 setup 完成、或已經開始拆解時 Component 是 null;
                // 這裡是走訪鏈的中途,呼叫端擋得掉 addon 為 null,擋不掉這個。
                var component = componentNode->Component;
                if(component == null)
                    return null;

                // NodeList 在 UldManager 配置節點清單之前是 null、NodeListCount 是 0,
                // 直接取 NodeList[0] 等同解空指標/讀出界。
                var nodeList = component->UldManager.NodeList;
                if(nodeList == null || component->UldManager.NodeListCount == 0)
                    return null;

                childNode = nodeList[0];
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
    /// <returns>
    /// 最上層的節點。<paramref name="node"/> 為 <see langword="null"/> 時回 <see langword="null"/> ——
    /// ⚠️ 這是本函式<b>唯一</b>會回 null 的情況;傳入非 null 時必定回非 null(最少回傳自己)。
    /// </returns>
    /// <remarks>
    /// 🔴 原本無條件解 <c>node-&gt;ParentNode</c>,傳入 null 直接 AccessViolationException
    /// (corrupted-state exception,try/catch 與任何例外隔離包裝都攔不到)。
    /// 📌 呼叫端若把回傳值直接拿去解參考,崩潰位置會從本函式內移到呼叫端 —— 崩潰行為等價,
    /// 但呼叫端至少<b>有機會</b>判空。
    /// </remarks>
    public static unsafe AtkResNode* GetRootNode(AtkResNode* node)
    {
        if(node == null)
            return null;

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

    /// <summary>
    /// 安全取得 <c>addon-&gt;AtkValues[index]</c> 的<b>複本</b>。這是 AddonMaster 讀取 AtkValue 的統一入口。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>AtkUnitBase.AtkValues</c> 是<b>指標欄位</b>(偏移 0x178),長度在 <c>AtkValuesCount</c>
    /// (偏移 0x1E2,<see cref="ushort"/>)。AddonMaster 裡大量索引是寫死的(最高到 1062),
    /// 而 addon 剛開窗、切分頁、或台服版面與國際服不同時,實際長度可能遠小於那個索引。
    /// 無界讀本身就是讀陣列外的記憶體;讀出來的 8 個位元組若被當成 <c>String</c> 指標解參考,
    /// 就是 AccessViolationException —— 而 AVE 在 .NET Core 是 corrupted-state exception,
    /// <c>try</c>/<c>catch</c> 與任何例外隔離包裝都攔不到。
    /// <br/>
    /// 📌 回傳的是<b>複本</b>而不是 <c>ref</c>:呼叫端後續讀 <c>.Type</c> / <c>.String</c> 時
    /// 不會再碰一次原生記憶體,避免「檢查時是字串、使用時已被換掉」的 TOCTOU。
    /// <br/>
    /// ⚠️ <c>value = default</c> 是刻意的,<b>不能</b>寫成 <c>new AtkValue()</c> ——
    /// <see cref="AtkValue"/> 的無參數建構子會呼叫原生 <c>Ctor()</c>。
    /// </remarks>
    /// <returns>
    /// <see langword="false"/> 當 <paramref name="addon"/> 為 null、<c>AtkValues</c> 尚未配置、
    /// 或 <paramref name="index"/> 出界。此時 <paramref name="value"/> 是全零的 <c>Undefined</c>。
    /// </returns>
    public static bool TryGetAtkValue(AtkUnitBase* addon, int index, out AtkValue value)
    {
        value = default;
        if(addon == null || addon->AtkValues == null) return false;
        if(index < 0 || index >= addon->AtkValuesCount) return false;
        value = addon->AtkValues[index];
        return true;
    }

    /// <summary>
    /// 安全讀取 <c>AtkValues[index]</c> 的整數欄位。索引出界時回 <paramref name="fallback"/>。
    /// </summary>
    /// <remarks>
    /// 📌 這裡<b>刻意不檢查</b> <c>Type</c>:<see cref="AtkValue"/> 的數值欄位是同一個 union
    /// (全部在偏移 0x8),<c>Int</c>/<c>UInt</c>/<c>Bool</c> 讀的是同一批位元組。加上型別檢查會
    /// 改變既有呼叫端在型別不符時拿到的值 —— 那是回退既有行為。這裡只補<b>邊界</b>,
    /// 因為只有邊界能造成讀出界。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAtkValueInt(AtkUnitBase* addon, int index, int fallback = 0)
        => TryGetAtkValue(addon, index, out var value) ? value.Int : fallback;

    /// <inheritdoc cref="GetAtkValueInt"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetAtkValueUInt(AtkUnitBase* addon, int index, uint fallback = 0)
        => TryGetAtkValue(addon, index, out var value) ? value.UInt : fallback;

    /// <inheritdoc cref="GetAtkValueInt"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAtkValueBool(AtkUnitBase* addon, int index, bool fallback = false)
        => TryGetAtkValue(addon, index, out var value) ? value.Bool : fallback;

    /// <summary>
    /// 安全讀取 <c>AtkValues[index]</c> 的型別。索引出界時回 <see cref="ValueType.Undefined"/>(＝0)。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueType GetAtkValueType(AtkUnitBase* addon, int index)
        => TryGetAtkValue(addon, index, out var value) ? value.Type : ValueType.Undefined;

    /// <summary>
    /// 三重守衛版的 AtkValue 字串讀取:①索引在界內 ②型別真的是字串 ③字串指標非空。
    /// </summary>
    /// <remarks>
    /// 🔴 這三道缺任何一道都是實機崩潰過的形狀(2026-08-02 宇宙探索高難任務面板)。
    /// 型別不符時 <c>String.Value</c> 讀到的是 <c>Int</c>/<c>Float</c> 那幾個位元組被當成指標,
    /// 交給 <c>MemoryHelper.ReadSeStringNullTerminated</c> 掃 null 結尾就是 AccessViolation。
    /// <br/>
    /// ⚠️ <see cref="IsString(AtkValue)"/> 本身已經含 <c>String.HasValue</c>,這裡仍然把
    /// <c>String.Value == null</c> 明寫出來 —— 守衛的意圖要在呼叫點看得見,
    /// 而不是依賴另一個函式的實作細節。
    /// <br/>
    /// ⚠️ <b><c>ValueType.WideString</c> 刻意<u>不</u>接受</b>,即使 <see cref="IsString(AtkValue)"/> 接受它。
    /// WideString 是 UTF-16,交給 UTF-8 的 <c>ReadSeStringNullTerminated</c> 只會得到亂碼或
    /// 被第一個 <c>0x00</c> 截斷的單字元 —— 沒有任何呼叫端能有意義地消費那個值。
    /// 📌 這也<b>正好保住</b> <c>WKSMission</c>／<c>WKSRecipeNotebook</c> 原本就寫死的較窄集合
    /// (<c>String</c>／<c>ManagedString</c>／<c>String8</c>,不含 WideString):
    /// 那兩處遇到非字串會 <c>break</c> 收尾,若改用較寬的 <c>IsString()</c> 就會多列出一筆亂碼項目。
    /// </remarks>
    public static bool TryGetAtkValueSeString(AtkUnitBase* addon, int index, out SeString result)
    {
        result = null!;
        if(!TryGetAtkValue(addon, index, out var value)) return false;
        if(value.Type != ValueType.String && value.Type != ValueType.String8 && value.Type != ValueType.ManagedString) return false;
        if(value.String.Value == null) return false;
        var read = MemoryHelper.ReadSeStringNullTerminated((nint)value.String.Value);
        if(read == null) return false;
        result = read;
        return true;
    }

    /// <summary>
    /// <see cref="TryGetAtkValueSeString"/> 的表達式版本。讀不到時回 <see langword="null"/> ——
    /// 呼叫端能區分「面板還沒載入」與「載入了但是空字串」。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString? GetAtkValueSeString(AtkUnitBase* addon, int index)
        => TryGetAtkValueSeString(addon, index, out var result) ? result : null;

    /// <inheritdoc cref="GetAtkValueSeString"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetAtkValueTextOrNull(AtkUnitBase* addon, int index)
        => TryGetAtkValueSeString(addon, index, out var result) ? result.GetText() : null;

    /// <summary>
    /// <see cref="GetAtkValueTextOrNull"/> 的不可空版本,讀不到時回<b>空字串</b>。
    /// 給既有簽章就是 <see cref="string"/>(非可空)的存取子用,避免改簽章回退呼叫端。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAtkValueText(AtkUnitBase* addon, int index)
        => GetAtkValueTextOrNull(addon, index) ?? "";

    /// <summary>
    /// 守衛版的 <c>PopupMenu.EntryNames[index]</c> 讀取(SelectString / SelectIconString 的條目文字)。
    /// </summary>
    /// <remarks>
    /// 🔴 這是 AtkValue 字串以外的<b>同構形狀</b>:<c>PopupMenu.EntryNames</c> 是
    /// <c>CStringPointer*</c>(偏移 0x10 的<b>指標欄位</b>),長度在 <c>EntryCount</c>(偏移 0x4C)。
    /// 選單建好之前 <c>EntryNames</c> 是 null,而條目數是<b>執行期</b>決定的 ——
    /// 原寫法 <c>EntryNames[Index].Value</c> 既沒判空也沒對 <c>EntryCount</c> 做邊界檢查,
    /// 兩者都會把垃圾位址交給 <c>MemoryHelper</c> 掃 null 結尾 = AccessViolationException,
    /// 而 AVE 在 .NET Core 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
    /// <br/>
    /// ⚠️ 呼叫端取 <c>&amp;addon-&gt;PopupMenu.PopupMenu</c> 之前<b>必須自己先判 addon 非空</b> ——
    /// 取欄位位址不會解參考,null 會靜默算成一個長得像小整數的毒指標,
    /// 本函式的 <c>menu == null</c> 擋不到它。
    /// </remarks>
    public static bool TryGetPopupMenuEntryName(FFXIVClientStructs.FFXIV.Client.UI.PopupMenu* menu, int index, out SeString result)
    {
        result = null!;
        if(menu == null || menu->EntryNames == null) return false;
        if(index < 0 || index >= menu->EntryCount) return false;
        var entry = menu->EntryNames[index].Value;
        if(entry == null) return false;
        var read = MemoryHelper.ReadSeStringNullTerminated((nint)entry);
        if(read == null) return false;
        result = read;
        return true;
    }

    /// <inheritdoc cref="TryGetPopupMenuEntryName"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SeString? GetPopupMenuEntryName(FFXIVClientStructs.FFXIV.Client.UI.PopupMenu* menu, int index)
        => TryGetPopupMenuEntryName(menu, index, out var result) ? result : null;
}
