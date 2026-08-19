using ECommons.Automation;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class FreeCompanyCreditShop : AddonMasterBase<AtkUnitBase>
    {
        public FreeCompanyCreditShop(nint addon) : base(addon) { }
        public FreeCompanyCreditShop(void* addon) : base(addon) { }

        /// <remarks>索引出界時回 0,見 <see cref="GenericHelpers.GetAtkValueInt"/>。</remarks>
        public uint FreeCompanyRank => GenericHelpers.GetAtkValueUInt(Addon, 0);

        /// <inheritdoc cref="FreeCompanyRank"/>
        public bool Unk01 => GenericHelpers.GetAtkValueBool(Addon, 1);

        /// <inheritdoc cref="FreeCompanyRank"/>
        public uint CompanyCredits => GenericHelpers.GetAtkValueUInt(Addon, 3);

        /// <inheritdoc cref="FreeCompanyRank"/>
        public bool Unk05 => GenericHelpers.GetAtkValueBool(Addon, 5);

        /// <remarks>
        /// 索引出界時回 0 —— <see cref="Items"/> 因此得到空陣列而不是用垃圾長度去配置。
        /// </remarks>
        public uint ItemCount => GenericHelpers.GetAtkValueUInt(Addon, 9);

        public Item[] Items
        {
            get
            {
                var ret = new Item[ItemCount];
                for(var i = 0; i < ret.Length; i++)
                    ret[i] = new(this, i);
                return ret;
            }
        }

        public override string AddonDescription { get; } = "Free Company credit shop window";

        public readonly struct Item
        {
            public int Index { get; init; }
            public string ItemName { get; init; }
            public uint ItemId { get; init; }
            public int IconId { get; init; }
            public uint Rank { get; init; }
            public int QuantityInInventory { get; init; }
            public int MaxPurchaseSize { get; init; }
            public uint Price { get; init; }
            private readonly FreeCompanyCreditShop Am;

            public Item(FreeCompanyCreditShop am, int index)
            {
                Am = am;
                Index = index;
                // 🔴 全部改走 GenericHelpers 的界內存取。原寫法對 ItemName 的 String.Value 無檢查
                // 直接解參考(型別不符 = 垃圾指標 = 攔不到的 AccessViolationException),
                // 其餘欄位則是無界讀。ItemName 讀不到時回空字串,其餘回 0。
                ItemName = GenericHelpers.GetAtkValueText(Am.Addon, 10 + index);
                ItemId = GenericHelpers.GetAtkValueUInt(Am.Addon, 30 + index);
                IconId = GenericHelpers.GetAtkValueInt(Am.Addon, 50 + index);
                Rank = GenericHelpers.GetAtkValueUInt(Am.Addon, 70 + index);
                QuantityInInventory = (int)GenericHelpers.GetAtkValueUInt(Am.Addon, 90 + index);
                MaxPurchaseSize = GenericHelpers.GetAtkValueInt(Am.Addon, 110 + index);
                Price = GenericHelpers.GetAtkValueUInt(Am.Addon, 130 + index); // for a single unit
            }

            public readonly void Buy(int quantity)
            {
                if(quantity <= MaxPurchaseSize)
                {
                    if(quantity * Price <= Am.CompanyCredits)
                        Callback.Fire(Am.Addon, true, 0, Index, quantity);
                    else
                        PluginLog.LogError($"Unable to purchase {quantity}x of {ItemId}. Insufficient company credits (requires {quantity * Price}, have {Am.CompanyCredits})");
                }
                else
                    PluginLog.LogError($"Unable to purchase {quantity}x of {ItemId}. Quantity exceeds max purchase size of {MaxPurchaseSize}");
            }

            public override readonly string? ToString() => $"{nameof(AddonMaster)}.{nameof(FreeCompanyCreditShop)}.{nameof(Item)} [{nameof(ItemId)}={ItemId} {nameof(ItemName)}=\"{ItemName}\", {nameof(Index)}={Index}]";
        }

        public void Buy(uint itemId, int quantity)
        {
            if(Items.TryGetFirst(x => x.ItemId == itemId, out var item))
                item.Buy(quantity);
            else
                PluginLog.LogError($"Item id \"{itemId}\" not found in {nameof(FreeCompanyCreditShop)}.{nameof(Items)}");
        }
    }
}
