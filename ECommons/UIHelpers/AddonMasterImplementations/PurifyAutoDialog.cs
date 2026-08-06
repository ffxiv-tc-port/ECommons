using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;


namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class PurifyAutoDialog : AddonMasterBase<AtkUnitBase>
    {
        public PurifyAutoDialog(nint addon) : base(addon) { }

        public PurifyAutoDialog(void* addon) : base(addon) { }

        public AtkComponentButton* CancelExitButton => Addon->GetComponentButtonById(16);
        public SeString CancelExitButtonSeString => GenericHelpers.ReadSeString(&CancelExitButton->UldManager.SearchNodeById(2)->GetAsAtkTextNode()->NodeText);
        public string CancelExitButtonText => CancelExitButtonSeString.GetText();
        /// <remarks>
        /// 🔴 <c>GetRow</c> 對不存在的列<b>擲例外</b>不回 null,所以原本那個 <c>!</c> 是無效的防護。
        /// 這裡的 3868/3869 是寫死的 Addon 列號,而寫死的資料表列號在台服一律要當成「可能不存在」
        /// (2026-08-06 離線驗過台服 7.20 的 Addon 表有這兩列,但那是<b>當下</b>的事實不是保證)。
        /// 讀不到時回 <see langword="false"/> —— 失敗形式是「按鈕文字比對不成立 → 不動作」,
        /// 不是把例外丟進呼叫端的每幀迴圈。
        /// </remarks>
        public bool PurificationActive => Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(3868)?.Text.ToString().Equals(CancelExitButtonText) == true;
        /// <inheritdoc cref="PurificationActive"/>
        public bool PurificationInactive => Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(3869)?.Text.ToString().Equals(CancelExitButtonText) == true;

        public override string AddonDescription { get; } = "Aetherial Reduction Dialog";

        public void CancelExit() => ClickButtonIfEnabled(CancelExitButton);
    }
}
