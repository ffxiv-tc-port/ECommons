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

                // 🔴 原本寫 NodeListCount > 3 卻去索引 NodeList[7] —— 半套邊界檢查:count 落在 4~7 時
                // 照樣越界讀,而讀到的是相鄰記憶體不是 null,失敗完全靜默。索引 7 需要 count > 7。
                if(Base->UldManager.NodeListCount <= 7)
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

        public AtkComponentButton* StartButton => Base->GetComponentButtonById(4);
        public AtkComponentButton* DataCenterButton => Base->GetComponentButtonById(5);
        public AtkComponentButton* MoviesAndTitlesButton => Base->GetComponentButtonById(6);
        public AtkComponentButton* OptionsButton => Base->GetComponentButtonById(7);
        public AtkComponentButton* LicenseButton => Base->GetComponentButtonById(8);
        public AtkComponentButton* ExitButton => Base->GetComponentButtonById(9);

        public override string AddonDescription { get; } = "Title menu";

        public void Start() => ClickButtonIfEnabled(StartButton);
        public void DataCenter() => ClickButtonIfEnabled(DataCenterButton);
        public void MoviesAndTitles() => ClickButtonIfEnabled(MoviesAndTitlesButton);
        public void Options() => ClickButtonIfEnabled(OptionsButton);
        public void License() => ClickButtonIfEnabled(LicenseButton);
        public void Exit() => ClickButtonIfEnabled(ExitButton);
    }
}
