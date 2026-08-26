using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Linq;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class SelectYesno : AddonMasterBase<AddonSelectYesno>
    {
        public SelectYesno(nint addon) : base(addon) { }
        public SelectYesno(void* addon) : base(addon) { }

        /// <remarks>
        /// 🔴 <c>PromptText</c> 是偏移 0x238 的<b>指標欄位</b>,開窗途中/版面未建好時為 null。
        /// 原本的 <c>ReadSeString(&amp;Addon->PromptText->NodeText)</c> 是<b>假守衛</b>:
        /// <c>ReadSeString</c> 內部雖有 <c>utf8String != null</c>,但 <c>NodeText</c> 在
        /// <c>AtkTextNode</c> 偏移 0xC0 —— 節點為 null 時 <c>&amp;node->NodeText</c> 不會當場崩,
        /// 而是靜默算出毒指標 0xC0,那個判空<b>照樣通過</b>,直到 <c>AsSpan()</c> 讀位址 0xC0 才炸,
        /// 崩潰現場完全指不到真因。取不到節點時回空字串(讀取型存取子⇒安靜回預設值,不寫 log)。
        /// </remarks>
        public SeString SeString
        {
            get
            {
                var node = Addon->PromptText;
                return node == null ? string.Empty : GenericHelpers.ReadSeString(&node->NodeText);
            }
        }
        /// <remarks>
        /// 🔴 <see cref="SeString"/> 的姊妹屬性,原本同樣沒有任何守衛:
        /// <c>AtkValues[0]</c> 在 addon 剛開窗時可能根本還沒配置(<c>AtkValuesCount</c> 為 0),
        /// 型別不符時 <c>String</c> 讀到的是別的欄位被當成指標 —— 兩者都是
        /// AccessViolationException,而 AVE 是 corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。
        /// 三道守衛見 <see cref="GenericHelpers.TryGetAtkValueSeString"/>;
        /// 讀不到時回空字串(維持既有的非可空簽章,<see cref="TextLegacy"/> 也才不會 NRE)。
        /// </remarks>
        public SeString SeStringNullTerminated => GenericHelpers.GetAtkValueSeString(Base, 0) ?? (SeString)string.Empty;
        public string Text => SeString.GetText();
        public string TextLegacy => string.Join(string.Empty, SeStringNullTerminated.Payloads.OfType<TextPayload>().Select(t => t.Text)).Replace('\n', ' ').Trim();

        /// <remarks>
        /// 索引出界時該格算成「不是字串」⇒ 不計入,見 <see cref="GenericHelpers.GetAtkValueType"/>。
        /// </remarks>
        public int ButtonsVisible => Enumerable.Range(1, 3).Count(x => GenericHelpers.GetAtkValueType(Base, x).EqualsAny(ValueType.String, ValueType.String8, ValueType.ManagedString, ValueType.WideString));

        public AtkComponentButton* ThirdButton => Addon->GetComponentButtonById(14);

        public override string AddonDescription { get; } = "Yes or No selection menu";

        public void Yes()
        {
            var yesButton = Addon->YesButton;
            // Both IsEnabled and the NodeFlags write below resolve through OwnerNode, which FFXIVClientStructs
            // does not null-check. GenericHelpers.IsComponentEnabled deliberately is NOT used for this test: it
            // reports "not enabled" for a null OwnerNode, which would send us into the force-enable branch and
            // dereference that very null pointer.
            if(yesButton != null && yesButton->OwnerNode != null && !yesButton->IsEnabled)
            {
                Svc.Log.Debug($"{nameof(AddonSelectYesno)}: Force enabling yes button");
                var flagsPtr = (ushort*)&yesButton->AtkComponentBase.OwnerNode->AtkResNode.NodeFlags;
                *flagsPtr ^= 1 << 5;
            }
            ClickButtonIfEnabled(yesButton);
        }

        /// <summary>
        /// This is always the second button. In a two button SelectYesno, this is no. In a three button SelectYesno, it can be something else (such as "Wait")
        /// </summary>
        public void No() => ClickButtonIfEnabled(Addon->NoButton);
        public void Third() => ClickButtonIfEnabled(ThirdButton);
    }
}

[Obsolete("Please use AddonMaster.SelectYesno")]
public unsafe class SelectYesnoMaster : AddonMaster.SelectYesno
{
    public SelectYesnoMaster(nint addon) : base(addon)
    {
    }

    public SelectYesnoMaster(void* addon) : base(addon)
    {
    }
}
