using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;


namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class SalvageAutoDialog : AddonMasterBase<AtkUnitBase>
    {
        public SalvageAutoDialog(nint addon) : base(addon) { }

        public SalvageAutoDialog(void* addon) : base(addon) { }

        public AtkComponentButton* EndDesynthesisButton => ButtonByIdOrNull(28);

        /// <remarks>
        /// 🔴 <c>GetComponentButtonById(28)</c>、<c>SearchNodeById(2)</c>、<c>GetAsAtkTextNode()</c> <b>每一跳都會合法回 null</b>:
        /// 前兩跳為 null 時 <c>&amp;…->NodeText</c> 不會當場崩,只是靜默算出毒指標(<c>NodeText</c> 在 <c>AtkTextNode</c> 偏移 0xC0),
        /// 連 <c>ReadSeString</c> 內部的 <c>!= null</c> 都騙得過去;<c>GetAsAtkTextNode()</c> 是 <c>[MemberFunction]</c>,
        /// 對 null 呼叫是當場 AccessViolation(<c>try</c>/<c>catch</c> 攔不到)。
        /// 任何一跳取不到就回空字串:下游 <see cref="DesynthesisActive"/>/<see cref="DesynthesisInactive"/>
        /// 比對的 Addon 列文字皆非空,空字串一律比不中⇒兩者同時為 <see langword="false"/>(fail-closed,不動作)。
        /// </remarks>
        public SeString EndDesynthesisButtonSeString
        {
            get
            {
                var button = EndDesynthesisButton;
                if(button == null) return string.Empty;
                // 後兩跳的位址沒解出時同樣當成取不到(回空字串＝比不中、不動作)。
                if(!CSAddress.Resolved(AtkUldManager.MemberFunctionPointers.SearchNodeById) || !CSAddress.Resolved(AtkResNode.MemberFunctionPointers.GetAsAtkTextNode))
                    return string.Empty;
                var node = button->UldManager.SearchNodeById(2);
                if(node == null) return string.Empty;
                var textNode = node->GetAsAtkTextNode();
                if(textNode == null) return string.Empty;
                return GenericHelpers.ReadSeString(&textNode->NodeText);
            }
        }
        public string EndDesynthesisButtonText => EndDesynthesisButtonSeString.GetText();
        /// <remarks>
        /// 🔴 <c>GetRow</c> 對不存在的列<b>擲例外</b>不回 null,所以用 <c>GetRowOrDefault</c>。
        /// 5867/5868 是寫死的 Addon 列號,而寫死的資料表列號在台服一律要當成「可能不存在」。
        /// 讀不到時回 <see langword="false"/> —— 失敗形式是「按鈕文字比對不成立 → 不動作」,
        /// 不是把例外丟進呼叫端的每幀迴圈。
        /// </remarks>
        public bool DesynthesisActive => Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(5867)?.Text.ToString().Equals(EndDesynthesisButtonText) == true;
        /// <inheritdoc cref="DesynthesisActive"/>
        public bool DesynthesisInactive => Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(5868)?.Text.ToString().Equals(EndDesynthesisButtonText) == true;

        public override string AddonDescription { get; } = "Desynthesis Bulk Dialog";

        public void EndDesynthesis() => ClickButtonIfEnabled(EndDesynthesisButton);
    }
}
