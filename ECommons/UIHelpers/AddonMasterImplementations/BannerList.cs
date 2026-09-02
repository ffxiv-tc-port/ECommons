using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    /// <summary>
    /// Portraits list addon
    /// </summary>
    public unsafe class BannerList : AddonMasterBase<AtkUnitBase>
    {
        public BannerList(nint addon) : base(addon) { }
        public BannerList(void* addon) : base(addon) { }

        public AtkComponentButton* EditButton => Addon->GetComponentButtonById(2);
        public AtkComponentButton* DisplayHelpButton => Addon->GetComponentButtonById(8);
        /// <remarks>
        /// 原本寫 node 34 —— 那是按鈕左邊那條 <c>Text</c> 標籤，不是按鈕本身。
        /// 非元件節點會讓 <c>GetComponentButtonById</c> 回 null，
        /// <c>UseAsInstantPortrait()</c> 因此是**完全靜默的空操作**。
        /// <c>ui/uld/BannerList.uld</c> 裡 node 33（Res 322x35）底下只有兩個子節點：
        /// node 34＝Text 240x35 標籤、node 35＝Button 70x35，
        /// 而 node 35 的元件貼圖來自 <c>ui/uld/ToggleButton.tex</c>，
        /// 兩個 20x20 圖示分別是「肖像」與「帶標記的肖像」＝即時肖像切換。
        /// </remarks>
        public AtkComponentButton* UseAsInstantPortraitButton => Addon->GetComponentButtonById(35);

        /// <remarks>索引出界時回 0,見 <see cref="GenericHelpers.GetAtkValueInt"/>。</remarks>
        public int NumPortraits => GenericHelpers.GetAtkValueInt(Addon, 17);

        /// <inheritdoc cref="NumPortraits"/>
        public int SelectedPortrait => GenericHelpers.GetAtkValueInt(Addon, 18); // 0 indexed

        /// <inheritdoc cref="NumPortraits"/>
        public int CharacterOption1IconId => GenericHelpers.GetAtkValueInt(Addon, 6);

        /// <inheritdoc cref="NumPortraits"/>
        public int CharacterOption2IconId => GenericHelpers.GetAtkValueInt(Addon, 8);

        /// <inheritdoc cref="NumPortraits"/>
        public int BackgroundIconId => GenericHelpers.GetAtkValueInt(Addon, 10);

        /// <inheritdoc cref="NumPortraits"/>
        public int FrameIconId => GenericHelpers.GetAtkValueInt(Addon, 12);

        /// <inheritdoc cref="NumPortraits"/>
        public int AccentIconId => GenericHelpers.GetAtkValueInt(Addon, 14);

        /// <remarks>
        /// ⚠️ <see cref="NumPortraits"/> 是從 AtkValue 讀出來的,面板未載入時可能是負值 ——
        /// <c>new Portraits[負數]</c> 會擲 <see cref="System.OverflowException"/>。夾成 0(＝空清單)。
        /// </remarks>
        public Portraits[] Portrait
        {
            get
            {
                var count = NumPortraits;
                if(count <= 0)
                    return [];

                var ret = new Portraits[count];
                for(var i = 0; i < ret.Length; i++)
                    ret[i] = new(Addon, GenericHelpers.GetAtkValueInt(Addon, 23 + 7 * i));
                return ret;
            }
        }

        public override string AddonDescription { get; } = "Portraits list";

        public readonly struct Portraits
        {
            private readonly AtkUnitBase* Addon;
            public uint Unk01 { get; init; } // always 0?
            public int ClassJobIconId { get; init; }

            /// <summary>
            /// 1-based index
            /// </summary>
            public int ListIndex { get; init; }
            public int GlamourPlateId { get; init; } // 0 = no link

            /// <summary>
            /// 1 = broken <br></br>
            /// 0 = not broken
            /// </summary>
            public int PortraitBroken { get; init; }

            /// <summary>
            /// 7 = unable to retrieve glamour plate data <br></br>
            /// 5 = broken portrait <br></br>
            /// 1 = unbroken portrait and UseAsInstantPortrait is off <br></br>
            /// 0 = unbroken portrait and UseAsInstantPortrait is on
            /// </summary>
            public int Unk06 { get; init; }

            /// <summary>
            /// 0 = on <br></br>
            /// 1 = off
            /// </summary>
            public int UseAsInstantPortrait { get; init; }
            public SeString GearSetName { get; init; }
            public SeString GearSetILvl { get; init; }

            public bool IsPortraitBroken => PortraitBroken == 1;
            public bool IsUseAsInstantPortraitSet => UseAsInstantPortrait == 0;

            public Portraits(AtkUnitBase* addon, int index)
            {
                Addon = addon;
                ListIndex = index;

                var offset = 7 * (ListIndex - 1);
                Unk01 = GenericHelpers.GetAtkValueUInt(Addon, 21 + offset);
                ClassJobIconId = GenericHelpers.GetAtkValueInt(Addon, 22 + offset);
                GlamourPlateId = GenericHelpers.GetAtkValueInt(Addon, 24 + offset);
                PortraitBroken = GenericHelpers.GetAtkValueInt(Addon, 25 + offset);
                Unk06 = GenericHelpers.GetAtkValueInt(Addon, 26 + offset);
                UseAsInstantPortrait = GenericHelpers.GetAtkValueInt(Addon, 27 + offset);

                // 🔴 索引 791/792 是全檔最大的寫死索引,面板未載滿時遠遠出界;
                // 原寫法還對 String.Value 無檢查直接解參考(型別不符 = 垃圾指標 =
                // 攔不到的 AccessViolationException)。三道守衛見 GenericHelpers.TryGetAtkValueSeString。
                // 讀不到時回空 SeString,維持既有的非可空欄位型別。
                var offset2 = 2 * (ListIndex - 1);
                GearSetName = GenericHelpers.GetAtkValueSeString(Addon, 791 + offset2) ?? (SeString)string.Empty;
                GearSetILvl = GenericHelpers.GetAtkValueSeString(Addon, 792 + offset2) ?? (SeString)string.Empty;
            }
        }

        public void Edit() => ClickButtonIfEnabled(EditButton);
        public void DisplayHelp() => ClickButtonIfEnabled(DisplayHelpButton);
        public void UseAsInstantPortrait() => ClickButtonIfEnabled(UseAsInstantPortraitButton);
    }
}
