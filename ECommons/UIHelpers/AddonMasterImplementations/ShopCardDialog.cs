using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class ShopCardDialog : AddonMasterBase<AddonShopCardDialog>
    {
        public ShopCardDialog(nint addon) : base(addon)
        {
        }

        public ShopCardDialog(void* addon) : base(addon)
        {
        }

        /// <remarks>
        /// 🔴 <c>GetTextNodeById</c> 找不到節點時<b>合法回 null</b>,而 <c>&amp;node->NodeText</c>
        /// 對 null 節點<b>不會當場崩</b>:<c>NodeText</c> 在 <c>AtkTextNode</c> 偏移 0xC0,
        /// 算出的毒指標 0xC0 連 <c>ReadSeString</c> 內部的 <c>!= null</c> 都騙得過去,
        /// 直到 <c>AsSpan()</c> 去讀位址 0xC0 才炸,崩潰現場完全指不到真因。
        /// 取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString Price
        {
            get
            {
                var node = Base->GetTextNodeById(10);
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        public int? Quantity { get => Addon->CardQuantityInput->Data.Value; set => Addon->CardQuantityInput->SetValue(value.HasValue ? (int)value : MinQuantity); }
        public int MinQuantity => Addon->CardQuantityInput->Data.Min;
        public int MaxQuantity => Addon->CardQuantityInput->Data.Max;

        public AtkComponentButton* SellButton => Base->GetComponentButtonById(16);
        public AtkComponentButton* CancelButton => Base->GetComponentButtonById(17);

        public override string AddonDescription { get; } = "Triple triad card exchange window";

        public void Sell() => ClickButtonIfEnabled(SellButton);
        public void Cancel() => ClickButtonIfEnabled(CancelButton);
    }
}
