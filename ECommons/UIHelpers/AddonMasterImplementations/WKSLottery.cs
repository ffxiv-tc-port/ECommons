using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
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

                    // 🔴 三重守衛(見 GenericHelpers.TryGetAtkValueSeString):型別不符時 String.Value 是垃圾指標,
                    // 解參考是攔不到的 AccessViolationException。
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

        // 選輪盤的兩發 callback:一次呼叫只送一發,呼叫端要重複呼叫才會走完兩步(只呼叫一次的只會送出第一發)。
        // 🔴 對關閉中的窗再送一發是原生 AccessViolation,而「第二發之前重新取址」與 IsVisible / IsReady 都擋不住:
        //    關掉的窗 GetAddonByName 照樣查得到,而且回的是同一個指標。所以第一發用 close:false(原生端不會關窗),
        //    送出之後立刻返回;同一個位址剛被送過就先跳過一段冷卻(牆鐘),下一次呼叫時位址是當場重新解出來的。
        // ⚠️ 假設不成立時的後果是「這一輪沒選到輪盤」或兩發順序錯,不是崩潰。參數組(0,0)與(1,0)沿用上游,沒被證實過。
        private static nint wheelLastAddon;
        private static long wheelLastTick;
        private static bool wheelFirstSent;

        /// <summary>同一個位址剛被送過 callback 之後的冷卻(毫秒)。要大於任何實際的幀長。</summary>
        private const int WheelCallbackCooldownMs = 100;

        /// <summary>
        /// 選定左邊的輪盤。<b>一次呼叫只送一發</b>,要走完兩步請重複呼叫(見上方註解)。
        /// </summary>
        public void SelectWheelLeft()
        {
            if(!TryGetReadyLotteryAddon(out var lottery))
            {
                wheelLastAddon = 0;
                wheelFirstSent = false;
                PluginLog.Debug("SelectWheelLeft: WKSLottery is not open");
                return;
            }

            // 🔴 位址只做等值比較,永遠不解參考它。
            var address = (nint)lottery;

            // 這扇窗剛被我們送過 → 這一輪什麼都不做。
            // 刻意不去問「它現在還好嗎」—— 那個問題離線與執行期都答不準。
            if(wheelLastAddon == address && Environment.TickCount64 - wheelLastTick < WheelCallbackCooldownMs)
                return;

            if(wheelLastAddon != address)
                wheelFirstSent = false;

            wheelLastAddon = address;
            wheelLastTick = Environment.TickCount64;

            if(!wheelFirstSent)
            {
                wheelFirstSent = true;
                // close:false ⇒ 原生端不會替我們關窗,所以這一發之後指標一定還是同一扇窗。
                Callback.Fire(lottery, false, 0, 0);
                return;
            }

            wheelFirstSent = false;
            Callback.Fire(lottery, true, 1, 0);
            // 之後不再碰 lottery(這一行只有常數字串)。
            PluginLog.Debug($"Selecting Left Wheel");
        }

        /// <summary>重新解析一次 <c>WKSLottery</c> 的位址,並確認它至少是開著且載入完成的。</summary>
        /// <remarks>
        /// 🔴 位址每次都當場重新取、不跨呼叫保存 —— 原生指標跨幀持有會靜默換人或懸空。
        /// ⚠️ <b>這裡的 <c>IsAddonReady</c> 不是關窗守衛</b>,它只擋「窗根本不在 / 還沒載完」;
        /// 「正在關閉中」擋不住(見上方註解)。真正擋那個的是一次一發加冷卻。
        /// </remarks>
        private static bool TryGetReadyLotteryAddon(out AtkUnitBase* addon)
        {
            addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("WKSLottery", 1).Address;
            return GenericHelpers.IsAddonReady(addon);
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