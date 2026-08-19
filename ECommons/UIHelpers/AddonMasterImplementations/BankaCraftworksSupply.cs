using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using System.Linq;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    /// <summary>
    /// Crystarium/Studium/Wachumeqimeqi Deliveries addon
    /// </summary>
    public unsafe class BankaCraftworksSupply : AddonMasterBase<AtkUnitBase>
    {
        public BankaCraftworksSupply(nint addon) : base(addon) { }
        public BankaCraftworksSupply(void* addon) : base(addon) { }

        /// <remarks>
        /// 索引出界時回 0,見 <see cref="GenericHelpers.GetAtkValueInt"/>。
        /// ⚠️ 0 不是合法道具 ID,<see cref="RequestedItemNumberAvailable"/> 因此回 0(＝沒有可交的)。
        /// </remarks>
        public uint CollectableItemId => GenericHelpers.GetAtkValueUInt(Addon, 8);

        public AtkComponentButton* DeliverButton => Addon->GetComponentButtonById(71);
        public AtkComponentButton* CancelButton => Addon->GetComponentButtonById(72);

        public void Deliver() => ClickButtonIfEnabled(DeliverButton);
        public void Cancel() => ClickButtonIfEnabled(CancelButton);

        /// <remarks>
        /// 🔴 <c>InventoryManager.Instance()</c> 是 <c>[StaticAddress(..., isPointer: true)]</c>,
        /// 回的是靜態位址裡存放的指標值,產生器只在<b>特徵碼失配</b>時擲例外、對回傳值不判空。
        /// 未登入/切場景時真的會是 null,直接 <c>-&gt;</c> 就是 AccessViolationException
        /// (corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到)。取不到時回 0。
        /// </remarks>
        public int RequestedItemNumberAvailable
        {
            get
            {
                var inventory = InventoryManager.Instance();
                return inventory == null ? 0 : inventory->GetInventoryItemCount(CollectableItemId);
            }
        }

        /// <remarks>
        /// 🔴 原本是<b>四連鎖裸解參考</b>:
        /// <c>GetComponentNodeById(...)-&gt;Component-&gt;UldManager.NodeList[1]-&gt;IsVisible()</c>。
        /// 鏈上每一環都合法回 null/未配置 —— 節點取得器找不到就回 null、元件 setup 完成前
        /// <c>Component</c> 是 null、<c>NodeList</c> 在配置前是 null 而 <c>NodeListCount</c> 是 0
        /// (索引 1 需要至少 2 個節點)、清單裡的元素本身也可能是 null。
        /// 任何一環為空都是 AccessViolationException,而 AVE 是 corrupted-state exception,
        /// <c>try</c>/<c>catch</c> 攔不到。
        /// <br/>
        /// 📌 讀不到的格子一律當成<b>未填</b> —— 這個方向是保守的:
        /// 誤判成「已填」會讓 <see cref="FirstUnfilledSlot"/> 少回一格而漏交。
        /// </remarks>
        public List<int> SlotsFilled
        {
            get
            {
                var ret = new List<int>();
                if(Addon == null)
                    return ret;

                for(var i = 0; i < 6; i++)
                {
                    var node = Addon->GetComponentNodeById((uint)(i + 36));
                    if(node == null)
                        continue;

                    var component = node->Component;
                    if(component == null)
                        continue;

                    var nodeList = component->UldManager.NodeList;
                    if(nodeList == null || component->UldManager.NodeListCount <= 1)
                        continue;

                    var child = nodeList[1];
                    if(child == null || !child->IsVisible())
                        continue;

                    ret.Add(i);
                }
                return ret;
            }
        }
        public int? FirstUnfilledSlot => SlotsFilled.Count == 6 ? null : Enumerable.Range(0, 6).FirstOrDefault(i => !SlotsFilled.Contains(i));

        public override string AddonDescription { get; } = "Crystarium/Studium/Wachumeqimeqi Deliveries window";

        public bool? TryHandOver(int slot)
        {
            if(SlotsFilled.Contains(slot)) return true;

            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu", 1).Address;

            if(contextMenu is null || !contextMenu->IsVisible)
            {
                Callback.Fire(Base, true, 2, slot);
                return false;
            }
            else
            {
                Callback.Fire(contextMenu, true, 0, 0, 1021003u, 0u, 0);
                PluginLog.Debug($"Filled slot {slot}");
                return true;
            }
        }
    }
}

