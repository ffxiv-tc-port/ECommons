using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace ECommons.UIHelpers.AddonMasterImplementations;

public partial class AddonMaster
{
    public unsafe class CollectablesShop : AddonMasterBase<AtkUnitBase>
    {
        public CollectablesShop(nint addon) : base(addon) { }

        public CollectablesShop(void* addon) : base(addon) { }

        public AtkComponentButton* TradeButton => Addon->GetComponentButtonById(51);
        /// <remarks>
        /// 🔴 <c>GetComponentNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>GetAsAtkComponent*()</c>
        /// 是 <c>[MemberFunction]</c> —— 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼,
        /// 當場 AccessViolation;AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 取不到就回 null;既有消費端 <c>ClickButtonIfEnabled</c>/<c>ClickCheckboxIfEnabled</c>
        /// 開頭都已判空,所以取得到時行為完全照舊。
        /// </remarks>
        private AtkComponentRadioButton* RadioButtonById(uint id)
        {
            var node = Addon->GetComponentNodeById(id);
            return node == null ? null : node->GetAsAtkComponentRadioButton();
        }

        public AtkComponentRadioButton* CarpenterButton => RadioButtonById(3);
        public AtkComponentRadioButton* BlacksmithButton => RadioButtonById(4);
        public AtkComponentRadioButton* ArmourerButton => RadioButtonById(5);
        public AtkComponentRadioButton* GoldsmithButton => RadioButtonById(6);
        public AtkComponentRadioButton* LeatherworkerButton => RadioButtonById(7);
        public AtkComponentRadioButton* WeaverButton => RadioButtonById(8);
        public AtkComponentRadioButton* AlchemistButton => RadioButtonById(9);
        public AtkComponentRadioButton* CulinarianButton => RadioButtonById(10);
        public AtkComponentRadioButton* MinerButton => RadioButtonById(11);
        public AtkComponentRadioButton* BotanistButton => RadioButtonById(12);
        public AtkComponentRadioButton* FisherButton => RadioButtonById(13);

        public override string AddonDescription { get; } = "Collectables";

        public void Trade() => ClickButtonIfEnabled(TradeButton);

        public bool SelectDiscipleTab(Job job) => SelectDiscipleTab((uint)job);
        public bool SelectDiscipleTab(uint classjob) => classjob is >= 8 and <= 18 ? ClickButtonIfEnabled(RadioButtonById(classjob - 5)) : throw new ArgumentOutOfRangeException(nameof(classjob));
    }
}
