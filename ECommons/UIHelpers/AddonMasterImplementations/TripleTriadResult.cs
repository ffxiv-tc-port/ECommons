using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class TripleTriadResult : AddonMasterBase<AtkUnitBase>
    {
        public TripleTriadResult(nint addon) : base(addon) { }
        public TripleTriadResult(void* addon) : base(addon) { }

        /// <summary>
        /// 0 = won, 1 = lost
        /// </summary>
        /// <remarks>
        /// 索引出界(addon 剛開窗/切頁時 <c>AtkValuesCount</c> 可能遠小於這裡寫死的索引)時回 0,
        /// 詳見 <see cref="GenericHelpers.GetAtkValueInt"/>。原本是無界讀,讀的是陣列外的記憶體。
        /// </remarks>
        public int WonValue => GenericHelpers.GetAtkValueInt(Addon, 2);
        public uint MGPReward => GenericHelpers.GetAtkValueUInt(Addon, 7);
        public bool WonGame => WonValue == 0;

        public AtkComponentButton* RematchButton => Addon->GetComponentButtonById(21);
        public AtkComponentButton* QuitButton => Addon->GetComponentButtonById(22);

        public override string AddonDescription { get; } = "Triple triad result window";

        public void Rematch() => ClickButtonIfEnabled(RematchButton);
        public void Quit() => ClickButtonIfEnabled(QuitButton);
    }
}
