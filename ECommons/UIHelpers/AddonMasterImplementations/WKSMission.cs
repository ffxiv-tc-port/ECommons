using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;

public partial class AddonMaster
{
    /// <summary>
    /// Space Exploration Mission Screen <br></br>
    /// Details all the missions that you can pick up/have done
    /// </summary>
    public unsafe partial class WKSMission : AddonMasterBase<AtkUnitBase>
    {
        public WKSMission(nint addon) : base(addon) { }
        public WKSMission(void* addon) : base(addon) { }

        public AtkComponentButton* HelpButton => Addon->GetComponentButtonById(7);
        public AtkComponentButton* MissionSelectionButton => Addon->GetComponentButtonById(8);
        public AtkComponentButton* MissionLogButton => Addon->GetComponentButtonById(9);
        public AtkComponentButton* BasicMissionsButton => Addon->GetComponentButtonById(13);
        public AtkComponentButton* ProvisionalMissionsButton => Addon->GetComponentButtonById(14);
        public AtkComponentButton* CriticalMissionsButton => Addon->GetComponentButtonById(15);

        /// <summary>
        /// Keeps the current number of missions that are displayed. <br></br>
        /// This includes the tabs seperating the missions by type [A, B, C, D]
        /// </summary>
        /// <remarks>
        /// 索引出界時回 0(＝一筆都不列),見 <see cref="GenericHelpers.GetAtkValueInt"/>。
        /// </remarks>
        public uint NumEntries => GenericHelpers.GetAtkValueUInt(Addon, 31);

        /// <inheritdoc cref="NumEntries"/>
        public uint SelectedMissionId => GenericHelpers.GetAtkValueUInt(Addon, 1061);

        /// <remarks>
        /// 🔴 索引 1062 遠大於面板剛開窗時的 <c>AtkValuesCount</c>,原寫法是無界讀;
        /// 而且只檢查 <c>Type</c>、沒有 <c>String.Value</c> 判空 —— 型別對但指標為空時
        /// 照樣是攔不到的 AccessViolationException。三道守衛見
        /// <see cref="GenericHelpers.TryGetAtkValueSeString"/>。
        /// 📌 讀不到時<b>維持既有的 "n/a"</b>,不改成空字串或 null(消費端可能在比對這個字面值)。
        /// </remarks>
        public string SelectedMissionName => GenericHelpers.GetAtkValueTextOrNull(Addon, 1062) ?? "n/a";

        public StellarMissions[] StellerMissions
        {
            get
            {
                var ret = new List<StellarMissions>();
                for(var i = 0; i < NumEntries; i++)
                {
                    var missionId = GenericHelpers.GetAtkValueUInt(Addon, 40 + i * 6);

                    // category header?
                    if(missionId == 0)
                        continue;

                    // 🔴 三重守衛(邊界／型別／指標判空)。原本只有 Type 檢查:
                    // 索引 802+ 在面板未載滿時是無界讀,型別對但指標為空時是攔不到的 AVE。
                    // 讀不到就當清單到此為止(與原本的 else break 同義)。
                    if(!GenericHelpers.TryGetAtkValueSeString(Addon, 802 + i * 2, out var missionName))
                        break;

                    var mission = new StellarMissions(this, i)
                    {
                        Name = missionName.GetText(),
                        MissionId = missionId
                    };
                    ret.Add(mission);
                }
                return [.. ret];
            }
        }

        public class StellarMissions(WKSMission master, int index)
        {
            public string Name { get; set; } = string.Empty;
            public uint MissionId;

            public void Select()
            {
                Callback.Fire(master.Base, true, 12, (int)MissionId, index);
            }
            public void Initiate()
            {
                Callback.Fire(master.Base, true, 13, (int)MissionId, index);
            }
        }

        public override string AddonDescription => "Steller Missions Ui";

        public void Help() => ClickButtonIfEnabled(HelpButton);
        public void MissionSelection() => ClickButtonIfEnabled(MissionSelectionButton);
        public void MissionLog() => ClickButtonIfEnabled(MissionLogButton);
        public void BasicMissions() => ClickButtonIfEnabled(BasicMissionsButton);
        public void ProvisionalMissions() => ClickButtonIfEnabled(ProvisionalMissionsButton);
        public void CriticalMissions() => ClickButtonIfEnabled(CriticalMissionsButton);
    }
}
