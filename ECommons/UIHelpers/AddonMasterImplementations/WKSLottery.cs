using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;

public partial class AddonMaster
{
    public unsafe partial class WKSLottery : AddonMasterBase<AtkUnitBase>
    {
        public WKSLottery(nint addon) : base(addon) { }
        public WKSLottery(void* addon) : base(addon) { }

        public AtkComponentButton* WheelLeftButton => Addon->GetComponentButtonById(29);
        public AtkComponentButton* WheelRightButton => Addon->GetComponentButtonById(39);
        public AtkComponentButton* SpinWheelButton => Addon->GetComponentButtonById(64);

        public WheelItems[] LeftWheelItems
        {
            get
            {
                var ret = new List<WheelItems>();
                for(var i = 0; i < 7; i++)
                {
                    var itemId = GenericHelpers.GetAtkValueUInt(Addon, 89 + i * 7);
                    if(itemId == 0)
                        continue;

                    var itemAmount = GenericHelpers.GetAtkValueUInt(Addon, 92 + i * 7);

                    // 🔴 三重守衛(見 GenericHelpers.TryGetAtkValueSeString)。原寫法對 String.Value
                    // 無檢查直接解參考:型別不符時讀到的是垃圾指標 = AccessViolationException,
                    // 而 AVE 是 corrupted-state exception,try/catch 攔不到。
                    // 讀不到名稱時整筆跳過:itemName 的欄位宣告是 required string,
                    // 塞空字串等於交出一個消費端無法辨識的假獎品。
                    if(!GenericHelpers.TryGetAtkValueSeString(Addon, 91 + i * 7, out var itemName))
                        continue;

                    var itemNameText = itemName.GetText();

                    var ItemList = new WheelItems()
                    {
                        itemId = itemId,
                        itemAmount = itemAmount,
                        itemName = itemNameText
                    };
                    ret.Add(ItemList);
                }
                return [.. ret];
            }
        }

        public WheelItems[] RightWheelItems
        {
            get
            {
                var ret = new List<WheelItems>();
                for(var i = 0; i < 7; i++)
                {
                    var itemId = GenericHelpers.GetAtkValueUInt(Addon, 138 + i * 7);
                    if(itemId == 0)
                        continue;

                    var itemAmount = GenericHelpers.GetAtkValueUInt(Addon, 141 + i * 7);

                    // 三重守衛,理由同 LeftWheelItems。
                    if(!GenericHelpers.TryGetAtkValueSeString(Addon, 140 + i * 7, out var itemName))
                        continue;

                    var itemNameText = itemName.GetText();

                    var ItemList = new WheelItems()
                    {
                        itemId = itemId,
                        itemAmount = itemAmount,
                        itemName = itemNameText
                    };
                    ret.Add(ItemList);
                }
                return [.. ret];
            }
        }

        public class WheelItems
        {
            public uint itemId;
            public required string itemName;
            public uint itemAmount;
        }

        public void SelectWheelLeft()
        {
            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("WKSLottery", 1).Address;

            Callback.Fire(contextMenu, true, 0, 0);
            Callback.Fire(contextMenu, true, 1, 0);
            PluginLog.Debug($"Selecting Left Wheel");
        }

        /* Not... fully implimented correctly. Need to figure out what's the callback for this one
        public void SelectWheelRight()
        {
            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("WKSLottery", 1);

            Callback.Fire(contextMenu, true, 0, 0);
            Callback.Fire(contextMenu, true, 1, 1);
            PluginLog.Debug($"Selecting Right Wheel");
        }
        */

        public void ConfirmButton() => ClickButtonIfEnabled(SpinWheelButton);


        public override string AddonDescription => "Steller Missions Lottery Ui";
    }
}