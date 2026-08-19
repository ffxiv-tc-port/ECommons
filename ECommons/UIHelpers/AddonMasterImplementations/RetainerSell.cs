using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class RetainerSell : AddonMasterBase<AddonRetainerSell>
    {
        public RetainerSell(nint addon) : base(addon) { }
        public RetainerSell(void* addon) : base(addon) { }

        public AtkComponentButton* ComparePricesButton => Addon->GetComponentButtonById(4);
        public AtkComponentButton* ConfirmButton => Addon->GetComponentButtonById(21);
        public AtkComponentButton* CancelButton => Addon->GetComponentButtonById(22);

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public int AskingPrice
        {
            get => GenericHelpers.GetAtkValueInt(Base, 5);
            set => Callback.Fire(Base, true, 2, value);
        }

        /// <inheritdoc cref="AskingPrice"/>
        public int Quantity
        {
            get => GenericHelpers.GetAtkValueInt(Base, 8);
            set => Callback.Fire(Base, true, 3, value);
        }

        /// <remarks>
        /// 🔴 節點取得器合法回 null,而 <c>->NodeText</c> 是內嵌值(偏移 0xC0):
        /// 對 null 節點取它就是解參考 null+0xC0 = AccessViolation,而 AVE 是
        /// corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。取不到時回空字串。
        /// </remarks>
        public string ItemName
        {
            get
            {
                var node = Addon->GetTextNodeById(7);
                return node == null ? string.Empty : node->NodeText.GetText();
            }
        }

        public override string AddonDescription { get; } = "Retainer item sell window";

        public void ComparePrices() => ClickButtonIfEnabled(ComparePricesButton);
        public void Confirm() => ClickButtonIfEnabled(ConfirmButton);
        public void Cancel() => ClickButtonIfEnabled(CancelButton);
    }
}
