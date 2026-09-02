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

        // ──────────────────────────────────────────────────────────────
        // 選輪盤的兩發 callback:一次呼叫只送一發
        // ──────────────────────────────────────────────────────────────
        // 🔴 為什麼不能像原本那樣一次送完(台服 7.20 逐指令,image base 0x140000000):
        //    Callback.Fire 的 updateState 就是原生 AtkUnitBase::FireCallback(0x1406422B0)
        //    的 close 參數。close 為真、且處理常式回傳的 AtkValue 非零時,0x1406423C4 起
        //    就會在**回到 C# 之前**走 vf6 Hide 或 vf4 Close。對關閉中的窗再送第二發是
        //    原生 AccessViolation,而 AVE 在 .NET Core 是 corrupted-state exception,
        //    try/catch 與任何例外隔離包裝都攔不到。
        //
        // 🔴 「第二發之前重新取址」擋不住這個(2026-09-03 離線證明):
        //    Svc.GameGui.GetAddonByName 走 AtkUnitManager.AllLoadedUnitsList
        //    (台服 0x14064B960 讀 +0x7108 的 count 與 +0x6908 的 entries),
        //    而 AtkUnitBase::Close(0x14063CFE0)只在 0x14063D00E 把自己從 +0x7920
        //    (UnitList16)移除 ⇒ **關掉的窗照樣查得到,而且回的是同一個指標**。
        //    IsVisible / IsReady 三關同樣不是證明(TCToolbox .82 實例:三關全過仍 AVE)。
        //
        // 🔑 所以這裡改成**完全不對窗的狀態做假設**的形狀,兩條各自獨立:
        //    ① 第一發改成 close:false。0x1406423B7 的 je 在 close 為 0 時直接跳過整個
        //      Hide/Close 區塊 ⇒ 那一發**原生端證明不可能關窗**(這是證明,不是緩解)。
        //    ② 送出之後立刻返回:同一個呼叫堆疊裡永遠只有一發。第二發要等**下一次呼叫**,
        //      而且同一個位址剛被送過的話一律先跳過一段冷卻(牆鐘,免疫 UI 隱藏與過場),
        //      屆時位址是當場重新解出來的。
        //    ⇒ 假設不成立時的後果是「這一輪沒選到輪盤」,**不是崩潰**。
        //
        // ⚠️ 刻意接受的代價:呼叫端要**重複呼叫**才會走完兩步。AddonMaster 的既有用法
        //    幾乎都是每幀輪詢的任務迴圈(ICE 的 Task_Gamba.GamblingTime 就是這種形狀),
        //    但只呼叫一次的呼叫端只會送出第一發。
        //
        // 📌 參數組(0,0)與(1,0)一個都沒改、也沒有刪掉任何一發 —— 那兩組值是上游作者
        //    試出來的(同檔被註解掉的 SelectWheelRight 還留著「還沒搞懂正確 callback」的自述),
        //    而艦隊內唯一真的在操作這扇窗的 ICE 改成直接寫兩顆輪盤鈕的 Flags、完全不送 callback
        //    ⇒ **這兩發到底對不對本身從來沒被證實過**。這次只負責讓它不會把遊戲弄崩。
        //
        // ⚠️ 位址會被重複使用:舊窗釋放後新窗可能落在同一個位址,那時步驟計數可能對不上,
        //    後果是先送了第二發(順序錯,不會崩)。冷卻窗遠短於一次 addon 生滅,實務上碰不到。
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