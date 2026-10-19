using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class ItemFinder : AddonMasterBase<AtkUnitBase>
    {
        public ItemFinder(nint addon) : base(addon) { }
        public ItemFinder(void* addon) : base(addon) { }

        /// <remarks>
        /// 按鈕是 node 17，不是 14 —— 14 是把描述框整個包起來的 <c>Res</c> 容器。非元件節點會讓
        /// <c>GetComponentButtonById</c> 回 null，而 <c>ClickButtonIfEnabled</c> 開頭又判空
        /// ⇒ <c>Close()</c> 會是完全靜默的空操作：不擲例外、不寫 log。
        /// </remarks>
        public AtkComponentButton* CloseButton => Addon->GetComponentButtonById(17);

        public override string AddonDescription { get; } = "Item search window";

        public void Close() => ClickButtonIfEnabled(CloseButton);
    }
}
