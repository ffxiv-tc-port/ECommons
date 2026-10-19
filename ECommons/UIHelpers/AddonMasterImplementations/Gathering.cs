using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Text.RegularExpressions;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe partial class Gathering : AddonMasterBase<AddonGathering>
    {
        public Gathering(nint addon) : base(addon) { }
        public Gathering(void* addon) : base(addon) { }

        public int CurrentIntegrity
        {
            get
            {
                // 🔴 GetTextNodeById 合法回 null;NodeText 是偏移 0xC0 的內嵌值⇒對 null 取它就是 AVE。
                var node = Addon->GetTextNodeById(9);
                if(node == null) return 0;
                var match = ExtractNumber().Match(node->NodeText.GetText());
                return match.Success ? int.Parse(match.Value) : 0;
            }
        }

        public int TotalIntegrity
        {
            get
            {
                // 🔴 GetTextNodeById 合法回 null;NodeText 是偏移 0xC0 的內嵌值⇒對 null 取它就是 AVE。
                var node = Addon->GetTextNodeById(12);
                if(node == null) return 0;
                var match = ExtractNumber().Match(node->NodeText.GetText());
                return match.Success ? int.Parse(match.Value) : 0;
            }
        }

        public GatheredItem[] GatheredItems
        {
            get
            {
                var gatheredItems = new GatheredItem[8];
                for(var i = 0; i < gatheredItems.Length; i++)
                {
                    gatheredItems[i] = new GatheredItem(this, Addon, GetCheckBox(i), i);
                }
                return gatheredItems;
            }
        }

        public override string AddonDescription { get; } = "Gathering window";

        public class GatheredItem
        {
            private Gathering addonMaster;
            private AddonGathering* addon;
            private AtkComponentCheckBox* checkbox;
            private int index;

            public GatheredItem(Gathering addonMaster, AddonGathering* addon, AtkComponentCheckBox* checkbox, int index)
            {
                this.addonMaster = addonMaster;
                this.addon = addon;
                this.checkbox = checkbox;
                this.index = index;
            }

            public AtkComponentCheckBox* CheckBox => checkbox;
            public bool IsEnabled => GenericHelpers.IsComponentEnabled(CheckBox);
            /// <remarks>
            /// 🔴 三跳全裸:<c>CheckBox</c> 來自 <c>GetCheckBox(index)</c>,轉手的是原生陣列槽,合法為 null;
            /// <c>GetTextNodeById</c> 合法回 null;<c>GetAsAtkTextNode()</c> 是 <c>[MemberFunction]</c>,
            /// 對 null 呼叫等於把 <c>this=0</c> 交給遊戲原生碼當場 AccessViolation(try/catch 攔不到)。
            /// 任何一跳取不到就回空字串。
            /// </remarks>
            public string ItemName
            {
                get
                {
                    var checkbox = CheckBox;
                    if(checkbox == null) return string.Empty;
                    var node = checkbox->GetTextNodeById(23);
                    if(node == null) return string.Empty;
                    var textNode = node->GetAsAtkTextNode();
                    return textNode == null ? string.Empty : textNode->NodeText.GetText();
                }
            }
            public uint ItemID => addon->ItemIds[index];
            public bool IsCollectable => Svc.Data.GetExcelSheet<Item>()?.GetRowOrDefault(ItemID)?.IsCollectable ?? false;
            public int ItemLevel
            {
                get
                {
                    // 🔴 三跳全裸(CheckBox / GetTextNodeById / GetAsAtkTextNode 各自合法回 null,
                    // 末端那跳是 [MemberFunction]⇒對 null 呼叫是 AVE,try/catch 攔不到)。取不到回 0。
                    var checkbox = CheckBox;
                    if(checkbox == null) return 0;
                    var node = checkbox->GetTextNodeById(21);
                    if(node == null) return 0;
                    var textNode = node->GetAsAtkTextNode();
                    if(textNode == null) return 0;
                    var match = ExtractNumber().Match(textNode->NodeText.GetText());
                    return match.Success ? int.Parse(match.Value) : 0;
                }
            }
            public int GatherChance
            {
                get
                {
                    // 🔴 三跳全裸(CheckBox / GetTextNodeById / GetAsAtkTextNode 各自合法回 null,
                    // 末端那跳是 [MemberFunction]⇒對 null 呼叫是 AVE,try/catch 攔不到)。取不到回 0。
                    var checkbox = CheckBox;
                    if(checkbox == null) return 0;
                    var node = checkbox->GetTextNodeById(10);
                    if(node == null) return 0;
                    var textNode = node->GetAsAtkTextNode();
                    if(textNode == null) return 0;
                    var match = ExtractNumber().Match(textNode->NodeText.GetText());
                    return match.Success ? int.Parse(match.Value) : 0;
                }
            }
            public int BoonChance
            {
                get
                {
                    // 🔴 三跳全裸(CheckBox / GetTextNodeById / GetAsAtkTextNode 各自合法回 null,
                    // 末端那跳是 [MemberFunction]⇒對 null 呼叫是 AVE,try/catch 攔不到)。取不到回 0。
                    var checkbox = CheckBox;
                    if(checkbox == null) return 0;
                    var node = checkbox->GetTextNodeById(16);
                    if(node == null) return 0;
                    var textNode = node->GetAsAtkTextNode();
                    if(textNode == null) return 0;
                    var match = ExtractNumber().Match(textNode->NodeText.GetText());
                    return match.Success ? int.Parse(match.Value) : 0;
                }
            }

            public void Gather() => addonMaster.ClickCheckboxIfEnabled(CheckBox);
        }

        private AtkComponentCheckBox* GetCheckBox(int index) => index switch
        {
            0 => Addon->GatheredItemComponentCheckbox[0],
            1 => Addon->GatheredItemComponentCheckbox[1],
            2 => Addon->GatheredItemComponentCheckbox[2],
            3 => Addon->GatheredItemComponentCheckbox[3],
            4 => Addon->GatheredItemComponentCheckbox[4],
            5 => Addon->GatheredItemComponentCheckbox[5],
            6 => Addon->GatheredItemComponentCheckbox[6],
            7 => Addon->GatheredItemComponentCheckbox[7],
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

        [GeneratedRegex(@"\d+")]
        private static partial Regex ExtractNumber();
    }
}
