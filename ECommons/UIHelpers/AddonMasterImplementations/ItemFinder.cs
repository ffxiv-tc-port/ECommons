using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class ItemFinder : AddonMasterBase<AtkUnitBase>
    {
        public ItemFinder(nint addon) : base(addon) { }
        public ItemFinder(void* addon) : base(addon) { }

        /// <remarks>
        /// 原本寫 node 14 —— 那是把描述框整個包起來的 <c>Res</c> 容器，不是按鈕。
        /// 台服 7.20 的原生 <c>AtkUnitBase::GetComponentButtonById</c> 會先擋
        /// <c>node-&gt;Type &lt; 1000</c>（非元件節點）直接回 null，而 <c>ClickButtonIfEnabled</c>
        /// 開頭又判空 ⇒ <c>Close()</c> 是**完全靜默的空操作**：不擲例外、不寫 log。
        /// <c>ui/uld/ItemFinder.uld</c> 整份只有一顆按鈕元件節點＝node 17
        /// （140x28，位於 670x670 視窗底部正中 (265,622)）。
        /// </remarks>
        public AtkComponentButton* CloseButton => Addon->GetComponentButtonById(17);

        public override string AddonDescription { get; } = "Item search window";

        public void Close() => ClickButtonIfEnabled(CloseButton);
    }
}
