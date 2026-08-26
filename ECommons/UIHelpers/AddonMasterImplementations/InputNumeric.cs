using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class InputNumeric : AddonMasterBase<AtkUnitBase>
    {
        public InputNumeric(nint addon) : base(addon) { }
        public InputNumeric(void* addon) : base(addon) { }

        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public uint Min => GenericHelpers.GetAtkValueUInt(Addon, 2);
        public uint Max => GenericHelpers.GetAtkValueUInt(Addon, 3);

        public AtkComponentButton* OkButton => Addon->GetComponentButtonById(4);
        public AtkComponentButton* CancelButton => Addon->GetComponentButtonById(5);

        public override string AddonDescription { get; } = "Number input dialogue";

        public void Ok(int value) => Callback.Fire(Addon, true, value);
        public void Ok() => ClickButtonIfEnabled(OkButton);
        public void Cancel() => ClickButtonIfEnabled(CancelButton);
    }
}
