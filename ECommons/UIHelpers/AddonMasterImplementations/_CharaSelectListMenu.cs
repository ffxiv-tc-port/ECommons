using ECommons.Automation;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public unsafe partial class AddonMaster
{
    public class _CharaSelectListMenu : AddonMasterBase
    {
        public _CharaSelectListMenu(nint addon) : base(addon) { }
        public _CharaSelectListMenu(void* addon) : base(addon) { }

        public AtkComponentButton* WorldButton => ButtonByIdOrNull(4);
        public AtkComponentButton* NewCharacterButton => ButtonByIdOrNull(5);
        public AtkComponentButton* BackUpClientSettingsButton => ButtonByIdOrNull(6);

        public void World() => ClickButtonIfEnabled(WorldButton);
        public void NewCharacter() => ClickButtonIfEnabled(NewCharacterButton);
        public void BackUpClientSettings() => ClickButtonIfEnabled(BackUpClientSettingsButton);

        public bool TemporarilyLocked => false;// AgentLobby.Instance()->TemporaryLocked;

        public void SelectWorld()
        {
            // 取不到 AtkStage 就完全不送事件,理由見 AddonMasterBase.TryCreateAtkEvent。
            if(!TryCreateAtkEvent(out var evt, 1)) return;
            var data = CreateAtkEventData().Build();
            Base->ReceiveEvent((AtkEventType)25, 1, &evt, &data);
        }

        public Character[] Characters
        {
            get
            {
                // 🔴 AgentLobby.Instance() 是 [Agent] 產生器產出的取得器，
                // 真的可能回 null（AgentModule 未就緒或 GetAgentByInternalId 沒拿到）。
                // 本屬性常在選角畫面進出時被輪詢，裸解參考＝AccessViolation（攔不到）。
                // 取得器要靠的位址沒解出時同樣當成取不到（它在那種情況是擲例外，不是回 null）。
                var agent = CSAddress.AgentChainResolved() ? AgentLobby.Instance() : null;
                if(agent == null) return [];
                var ret = new List<Character>();
                var charaSpan = agent->LobbyData.CharaSelectEntries.ToArray();
                for(var i = 0; i < charaSpan.Length; i++)
                {
                    var s = charaSpan[i];
                    ret.Add(new(this, i, s));
                }
                return [.. ret];
            }
        }

        public override string AddonDescription { get; } = "Character select menu";

        public class Character
        {
            private _CharaSelectListMenu Master;
            public int Index { get; init; }
            public CharaSelectCharacterEntry* Entry { get; init; }
            public string Name => Entry->NameString;
            public uint HomeWorld => Entry->HomeWorldId;
            public uint CurrentWorld => Entry->CurrentWorldId;
            public bool IsVisitingAnotherDC => Entry->LoginFlags.HasFlag(CharaSelectCharacterEntryLoginFlags.Unk32);
            public bool CanLoginNormally => !Entry->LoginFlags.HasFlag(CharaSelectCharacterEntryLoginFlags.Locked) && !Entry->LoginFlags.HasFlag(CharaSelectCharacterEntryLoginFlags.NameChangeRequired) && !Entry->LoginFlags.HasFlag(CharaSelectCharacterEntryLoginFlags.MissingExVersionForLogin) && !Entry->LoginFlags.HasFlag(CharaSelectCharacterEntryLoginFlags.Unk32);
            public bool IsSelected
            {
                get
                {
                    var agent = CSAddress.AgentChainResolved() ? AgentLobby.Instance() : null;
                    return agent != null && agent->HoveredCharacterContentId == Entry->ContentId;
                }
            }

            public Character(_CharaSelectListMenu master, int index, CharaSelectCharacterEntry* entry)
            {
                Master = master;
                Index = index;
                Entry = entry;
            }

            public void Select()
            {
                Callback.Fire(Master.Base, true, 21, Index);
            }

            public void Login() => Click(false);

            public void OpenContextMenu() => Click(true);

            private void Click(bool right)
            {
                var eventIndex = (byte)(5 + Index);
                // 取不到 AtkStage 就完全不送事件,理由見 AddonMasterBase.TryCreateAtkEvent。
                if(!Master.TryCreateAtkEvent(out var atkEvent, eventIndex)) return;
                var evt = stackalloc AtkEvent[] { atkEvent };
                var data = stackalloc AtkEventData[] { Master.CreateAtkEventData().Write<byte>(6, (byte)(right ? 1 : 0)).Build() };
                Master.Base->ReceiveEvent(AtkEventType.MouseClick, eventIndex, evt, data);
            }

            public override string? ToString()
            {
                return $"{Name}@{ExcelWorldHelper.GetName(HomeWorld)}";
            }
        }
    }
}
