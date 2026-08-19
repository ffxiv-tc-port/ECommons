using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;

public partial class AddonMaster
{
    /// <summary>
    /// Moon Recipe Notebook
    /// Works similary to the normal recipe, except it's specialized to moon items you're doing the quest for
    /// </summary>
    public unsafe partial class WKSRecipeNotebook : AddonMasterBase<AtkUnitBase>
    {
        public WKSRecipeNotebook(nint addon) : base(addon) { }
        public WKSRecipeNotebook(void* addon) : base(addon) { }

        public AtkComponentButton* NQItemsButton => Addon->GetComponentButtonById(39);
        public AtkComponentButton* HQItemsButton => Addon->GetComponentButtonById(40);
        public AtkComponentButton* SynthesizeButton => Addon->GetComponentButtonById(50);

        /// <remarks>
        /// 🔴 三重守衛(索引在界內／型別真的是字串／字串指標非空),
        /// 見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>。讀不到時回空字串。
        /// </remarks>
        public string SelectedCraftingItem => GenericHelpers.GetAtkValueText(Addon, 46);

        public CraftItems[] CraftingItems
        {
            get
            {
                var ret = new List<CraftItems>();
                for(var i = 0; i < 5; i++)
                {
                    // 🔴 原本只檢查 Type,沒有邊界檢查、也沒有 String.Value 判空 ——
                    // 型別對但指標為空時照樣是攔不到的 AccessViolationException。
                    // TryGetAtkValueSeString 三道一次做完;讀不到就當清單到此為止(與原本的 else break 同義)。
                    if(!GenericHelpers.TryGetAtkValueSeString(Addon, 35 + i * 2, out var itemName))
                        break;

                    var item = new CraftItems(this, i)
                    {
                        Name = itemName.GetText()
                    };
                    ret.Add(item);
                }
                return [.. ret];

            }
        }

        public class CraftItems(WKSRecipeNotebook master, int index)
        {
            public string Name { get; set; } = string.Empty;

            public void Select()
            {
                Callback.Fire(master.Base, true, 0, index);
            }
        }

        public void NQItemInput() => ClickButtonIfEnabled(NQItemsButton);
        public void HQItemInput() => ClickButtonIfEnabled(HQItemsButton);
        public void Synthesize() => ClickButtonIfEnabled(SynthesizeButton);

        public override string AddonDescription => "Crafting Addon for Cosmic Exploration";
    }
}
