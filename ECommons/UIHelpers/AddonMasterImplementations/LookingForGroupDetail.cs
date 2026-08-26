using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public unsafe partial class AddonMaster
{
    public class LookingForGroupDetail : AddonMasterBase<AddonLookingForGroupDetail>
    {
        public LookingForGroupDetail(nint addon) : base(addon) { }
        public LookingForGroupDetail(void* addon) : base(addon) { }

        public AtkComponentButton* JoinEditButton => Base->GetComponentButtonById(109);
        public AtkComponentButton* TellEndButton => Base->GetComponentButtonById(110);
        public AtkComponentButton* BackButton => Base->GetComponentButtonById(111);

        public bool JoinEdit() => ClickButtonIfEnabled(JoinEditButton);
        public bool TellEnd() => ClickButtonIfEnabled(TellEndButton);
        public bool Back() => ClickButtonIfEnabled(BackButton);

        /// <remarks>
        /// 🔴 節點取得器合法回 null,而 <c>->NodeText</c> 是內嵌值(偏移 0xC0):
        /// 對 null 節點取它就是解參考 null+0xC0 = AccessViolation,而 AVE 是
        /// corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。取不到時回空字串。
        /// </remarks>
        public string PartyLeader
        {
            get
            {
                var node = Addon->GetTextNodeById(6);
                return node == null ? string.Empty : node->NodeText.GetText();
            }
        }
        /// <remarks>
        /// 🔴 節點取得器合法回 null,而 <c>->NodeText</c> 是內嵌值(偏移 0xC0):
        /// 對 null 節點取它就是解參考 null+0xC0 = AccessViolation,而 AVE 是
        /// corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。取不到時回空字串。
        /// </remarks>
        public string Description
        {
            get
            {
                var node = Addon->GetTextNodeById(20);
                return node == null ? string.Empty : node->NodeText.GetText();
            }
        }
        /// <remarks>
        /// 🔴 節點取得器合法回 null,而 <c>->NodeText</c> 是內嵌值(偏移 0xC0):
        /// 對 null 節點取它就是解參考 null+0xC0 = AccessViolation,而 AVE 是
        /// corrupted-state exception,<c>try</c>/<c>catch</c> 攔不到。取不到時回空字串。
        /// </remarks>
        public string World
        {
            get
            {
                var node = Addon->GetTextNodeById(33);
                return node == null ? string.Empty : node->NodeText.GetText();
            }
        }

        public override string AddonDescription { get; } = "Party finder details window";
    }
}