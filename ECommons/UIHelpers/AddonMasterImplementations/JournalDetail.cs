using FFXIVClientStructs.FFXIV.Client.UI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class JournalDetail : AddonMasterBase<AddonJournalDetail>
    {
        public JournalDetail(nint addon) : base(addon)
        {
        }

        public JournalDetail(void* addon) : base(addon) { }

        public bool CanInitiate => Addon->InitiateButton != null
            && GenericHelpers.IsComponentVisible(&Addon->InitiateButton->AtkComponentBase)
            && GenericHelpers.IsComponentEnabled(Addon->InitiateButton);

        public override string AddonDescription { get; } = "Quest detail window";

        public void AcceptMap() => ClickButtonIfEnabled(Addon->AcceptMapButton);
        public void Initiate() => ClickButtonIfEnabled(Addon->InitiateButton);
        public void AbandonDecline() => ClickButtonIfEnabled(Addon->AbandonDeclineButton);
    }
}
