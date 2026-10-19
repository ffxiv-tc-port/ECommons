using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class _TitleMenu : AddonMasterBase
    {
        public _TitleMenu(nint addon) : base(addon)
        {
        }

        public _TitleMenu(void* addon) : base(addon)
        {
        }

        public bool IsReady
        {
            get
            {
                if(!GenericHelpers.IsScreenReady() || !GenericHelpers.IsAddonReady(Base))
                    return false;

                // 🔴 索引 NodeList[7] 需要 count > 7。門檻寫小了(半套邊界檢查)照樣越界讀,
                // 而讀到的是相鄰記憶體不是 null,失敗完全靜默。
                if(Base->UldManager.NodeListCount <= 7)
                    return false;

                // 位址沒解出時當成還沒就緒(等標題畫面的步驟會等到逾時,不會在沒確認的畫面上按)。
                if(!CSAddress.Resolved(AtkResNode.MemberFunctionPointers.IsVisible) || !CSAddress.Resolved(AtkUnitBase.MemberFunctionPointers.GetNodeById))
                    return false;

                var node7 = Base->UldManager.NodeList[7];
                if(node7 == null || !node7->IsVisible())
                    return false;

                // 🔴 GetNodeById 找不到節點時合法回 null,而 Color 是內嵌值⇒對 null 取它就是 AVE。
                var node3 = Base->GetNodeById(3);
                if(node3 == null || node3->Color.A != 0xFF)
                    return false;

                return !GenericHelpers.TryGetAddonByName<AtkUnitBase>("TitleDCWorldMap", out _)
                    && !GenericHelpers.TryGetAddonByName<AtkUnitBase>("TitleConnect", out _);
            }
        }

        public AtkComponentButton* StartButton => ButtonByIdOrNull(4);
        public AtkComponentButton* DataCenterButton => ButtonByIdOrNull(5);
        public AtkComponentButton* MoviesAndTitlesButton => ButtonByIdOrNull(6);
        public AtkComponentButton* OptionsButton => ButtonByIdOrNull(7);
        public AtkComponentButton* LicenseButton => ButtonByIdOrNull(8);
        public AtkComponentButton* ExitButton => ButtonByIdOrNull(9);

        public override string AddonDescription { get; } = "Title menu";

        public void Start() => ClickButtonIfEnabled(StartButton);
        public void DataCenter() => ClickButtonIfEnabled(DataCenterButton);
        public void MoviesAndTitles() => ClickButtonIfEnabled(MoviesAndTitlesButton);
        public void Options() => ClickButtonIfEnabled(OptionsButton);
        public void License() => ClickButtonIfEnabled(LicenseButton);
        public void Exit() => ClickButtonIfEnabled(ExitButton);
    }
}
