using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;

public partial class AddonMaster
{
    /// <summary>
    /// Moon Recipe Notebook
    /// Works similary to the normal recipe, except it's specialized to moon items you're doing the quest for
    /// </summary>
    public unsafe partial class WKSToolCustomize : AddonMasterBase<AtkUnitBase>
    {
        public WKSToolCustomize(nint addon) : base(addon) { }
        public WKSToolCustomize(void* addon) : base(addon) { }

        public ClassSelector[] ClassList
        {
            get
            {
                var ret = new List<ClassSelector>();
                for(var i = 0; i < 11; i++)
                {
                    var level = GenericHelpers.GetAtkValueUInt(Addon, 22 + i);
                    if(level == 0)
                        continue;

                    // 🔴 三重守衛,見 GenericHelpers.TryGetAtkValueSeString:原寫法對 String.Value
                    // 無檢查直接解參考,型別不符時讀到的是垃圾指標 = AccessViolationException(攔不到)。
                    // 名稱讀不出來就整筆跳過 —— 回空字串會讓消費端拿到一個無法選取的假項目。
                    if(!GenericHelpers.TryGetAtkValueSeString(Addon, 11 + i, out var className))
                        continue;

                    var ClassName = className.GetText();
                    var ClassList = new ClassSelector(this, i)
                    {
                        ClassName = ClassName,
                        Level = level,
                    };
                    ret.Add(ClassList);
                }
                return [.. ret];
            }
        }

        public class ClassSelector(WKSToolCustomize master, int index)
        {
            public required string ClassName;
            public uint Level;

            public void Select()
            {
                Callback.Fire(master.Base, true, 11, index);
            }
        }

        public override string AddonDescription => "Cosmic Relic Tool Ui";
    }
}
