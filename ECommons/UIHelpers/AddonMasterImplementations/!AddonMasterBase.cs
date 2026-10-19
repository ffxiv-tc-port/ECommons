using ECommons.Automation.UIInput;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using AtkEvent = FFXIVClientStructs.FFXIV.Component.GUI.AtkEvent;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public abstract unsafe class AddonMasterBase<T> : IAddonMasterBase where T : unmanaged
{
    protected AddonMasterBase(nint addon)
    {
        Addon = (T*)addon;
    }
    protected AddonMasterBase(void* addon)
    {
        Addon = (T*)addon;
    }

    /// <summary>
    /// User-friendly description, for use in plugin settings, etc.
    /// </summary>
    public abstract string AddonDescription { get; }
    public T* Addon { get; }
    public AtkUnitBase* Base => (AtkUnitBase*)Addon;
    public bool IsVisible => Base->IsVisible;
    public bool IsAddonReady => GenericHelpers.IsAddonReady(Base);

    // 節點取得器的不擲版本:CS 成員函式位址沒解出時回 null(與「找不到節點」同值);解出時就是原本那一個呼叫。
    // 刻意 private protected:不改公開介面,repo 外的子類別看不到。
    private protected AtkComponentButton* ButtonByIdOrNull(uint id)
        => CSAddress.Resolved(AtkUnitBase.MemberFunctionPointers.GetComponentButtonById) ? Base->GetComponentButtonById(id) : null;

    private protected AtkTextNode* TextNodeByIdOrNull(uint id)
        => CSAddress.Resolved(AtkUnitBase.MemberFunctionPointers.GetTextNodeById) ? Base->GetTextNodeById(id) : null;

    /// <remarks>
    /// 🔴 <c>RaptureAtkUnitManager.Instance()</c> 是 FFXIVClientStructs 裡<b>手寫</b>的取得器,
    /// <c>RaptureAtkModule.Instance()</c> 為 null 就回 null;<c>FocusedUnitsList</c> 是內嵌的值型別成員,
    /// 對 null 取它是 AccessViolation(corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到)。
    /// <c>AtkStage.Instance()</c> 同樣可能回 null:它宣告成 <c>[StaticAddress(..., isPointer: true)]</c>,
    /// 回的是靜態位址裡存放的指標值,產生器只在<b>特徵碼失配</b>時擲例外,對回傳值不判空。
    /// 取不到任何一個管理器時一律回 <see langword="false"/>(＝沒有焦點,保守)。
    /// </remarks>
    public bool HasFocus
    {
        get
        {
            var stage = CSAddress.AtkStageOrNull();
            if(stage == null) return false;
            if(!CSAddress.Resolved(AtkStage.MemberFunctionPointers.GetFocus)) return false;
            var focus = stage->GetFocus();
            if(focus == null) return false;
            var manager = CSAddress.UIModuleResolved() ? RaptureAtkUnitManager.Instance() : null;
            if(manager == null) return false;
            for(var i = 0; i < Math.Min((int)manager->FocusedUnitsList.Count, manager->FocusedUnitsList.Entries.Length); i++)
            {
                var atk = manager->FocusedUnitsList.Entries[i].Value;
                if(atk != null && atk->RootNode == GenericHelpers.GetRootNode(focus))
                    return true;
            }
            return false;
        }
    }

    /// <remarks>取不到 <c>RaptureAtkUnitManager</c> 時回 <see langword="false"/>,理由見 <see cref="HasFocus"/>。</remarks>
    public bool IsAddonInFocusList
    {
        get
        {
            var manager = CSAddress.UIModuleResolved() ? RaptureAtkUnitManager.Instance() : null;
            if(manager == null) return false;
            for(var i = 0; i < Math.Min((int)manager->FocusedUnitsList.Count, manager->FocusedUnitsList.Entries.Length); i++)
            {
                var atk = manager->FocusedUnitsList.Entries[i].Value;
                if(atk != null && atk == Base) return true;
            }
            return false;
        }
    }

    [Obsolete("For the intended functionality please use HasFocus. For the same functionality please use IsAddonInFocusList.")]
    public bool IsAddonFocused => IsAddonInFocusList;

    /// <remarks>取不到 <c>RaptureAtkUnitManager</c> 時回 <see langword="false"/>,理由見 <see cref="HasFocus"/>。</remarks>
    public bool IsAddonOnlyFocusListEntry
    {
        get
        {
            var manager = CSAddress.UIModuleResolved() ? RaptureAtkUnitManager.Instance() : null;
            if(manager == null) return false;
            return manager->FocusedUnitsList.Count == 1 && manager->FocusedUnitsList.Entries[0].Value == Base;
        }
    }

    // IsEnabled dereferences OwnerNode and IsVisible() dereferences AtkResNode, neither of which is null-checked
    // by FFXIVClientStructs. Both are routed through GenericHelpers so that a missing node is reported as
    // "not clickable" instead of raising an uncatchable AccessViolationException.
    protected bool ClickButtonIfEnabled(AtkComponentButton* button)
    {
        if(button == null) return false;
        if(GenericHelpers.IsComponentEnabled(button) && GenericHelpers.IsComponentVisible(&button->AtkComponentBase))
        {
            button->ClickAddonButton(Base);
            return true;
        }
        return false;
    }

    protected bool ClickButtonIfEnabled(AtkComponentRadioButton* button)
    {
        if(button == null) return false;
        if(GenericHelpers.IsComponentEnabled(button) && GenericHelpers.IsComponentVisible(&button->AtkComponentButton.AtkComponentBase))
        {
            button->ClickRadioButton(Base);
            return true;
        }
        return false;
    }

    protected bool ClickCheckboxIfEnabled(AtkComponentCheckBox* checkbox)
    {
        if(checkbox == null) return false;
        // SetChecked 的位址沒解出時整個不按:只按不改勾選狀態會留下半套操作。
        if(GenericHelpers.IsComponentEnabled(checkbox) && GenericHelpers.IsComponentVisible(&checkbox->AtkComponentButton.AtkComponentBase)
            && CSAddress.Resolved(AtkComponentButton.MemberFunctionPointers.SetChecked))
        {
            checkbox->ClickCheckBox(Base);
            checkbox->SetChecked(true);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 建立一個以本 addon 為 <c>Listener</c>、以 <c>AtkStage</c> 的事件目標為 <c>Target</c> 的事件。
    /// </summary>
    /// <remarks>
    /// 取不到 <c>AtkStage</c> 時<b>擲例外而不是回一個壞掉的事件</b>,理由見 <see cref="TryCreateAtkEvent"/>。
    /// 簽章刻意維持不變(艦隊內有 repo 外的子類別在呼叫它),想要安靜跳過的呼叫端改用
    /// <see cref="TryCreateAtkEvent"/>。
    /// </remarks>
    /// <exception cref="InvalidOperationException">取不到 <c>AtkStage</c>。</exception>
    protected AtkEvent CreateAtkEvent(byte flags = 0)
    {
        if(!TryCreateAtkEvent(out var atkEvent, flags))
            throw new InvalidOperationException("AtkStage is not available; refusing to build an AtkEvent with an invalid target.");
        return atkEvent;
    }

    /// <summary>
    /// <see cref="CreateAtkEvent"/> 的 fail-closed 版本:取不到 <c>AtkStage</c> 時回
    /// <see langword="false"/>,呼叫端應直接放棄送出事件。
    /// </summary>
    /// <remarks>
    /// 🔴 <c>Target = &amp;AtkStage.Instance()-&gt;AtkEventTarget</c> 取欄位位址<b>不會解參考</b>:
    /// <c>AtkStage.Instance()</c> 回 null 時算出的是 <c>null + offsetof(AtkEventTarget)</c>,一個長得像小整數的假指標,
    /// 被原封不動交給遊戲的 <c>ReceiveEvent</c>,在遊戲碼解參考它時才崩(AccessViolation,<c>try</c>/<c>catch</c> 攔不到)。
    /// ⇒ <b>失敗必須在送出事件之前攔下,不能靠例外處理善後。</b>
    /// <c>AtkStage.Instance()</c> 為什麼會回 null 見 <see cref="HasFocus"/>。
    /// </remarks>
    protected bool TryCreateAtkEvent(out AtkEvent atkEvent, byte flags = 0)
    {
        atkEvent = default;
        var stage = CSAddress.AtkStageOrNull();
        if(stage == null) return false;
        atkEvent = new AtkEvent()
        {
            Listener = (AtkEventListener*)Base,
            Target = &stage->AtkEventTarget,
            State = new()
            {
                StateFlags = (AtkEventStateFlags)flags
            }
        };
        return true;
    }

    protected AtkEventDataBuilder CreateAtkEventData()
    {
        return new();
    }
}

public abstract unsafe class AddonMasterBase : AddonMasterBase<AtkUnitBase>
{
    protected AddonMasterBase(nint addon) : base(addon)
    {
    }

    protected AddonMasterBase(void* addon) : base(addon)
    {
    }
}

public unsafe interface IAddonMasterBase
{
    string AddonDescription { get; }
    unsafe AtkUnitBase* Base { get; }
    bool HasFocus { get; }
    bool IsAddonInFocusList { get; }
    bool IsAddonOnlyFocusListEntry { get; }
    bool IsAddonReady { get; }
    bool IsVisible { get; }
}
