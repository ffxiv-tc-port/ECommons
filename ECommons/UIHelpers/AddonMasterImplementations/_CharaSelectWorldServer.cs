using Dalamud.Memory;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Callback = ECommons.Automation.Callback;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public unsafe partial class AddonMaster
{
    public class _CharaSelectWorldServer : AddonMasterBase
    {
        public _CharaSelectWorldServer(nint addon) : base(addon)
        {
        }

        public _CharaSelectWorldServer(void* addon) : base(addon)
        {
        }

        public World[] Worlds
        {
            get
            {
                var ret = new List<World>();
                // 🔴 RaptureAtkModule.Instance() 是 FFXIVClientStructs 裡手寫的取得器,本體就是
                // 「UIModule.Instance() 為 null 就回 null」—— 而本屬性正好是在登入畫面進出時被輪詢,
                // 那就是模組還沒建好的時機。裸解參考＝AccessViolation,攔不到。
                var module = RaptureAtkModule.Instance();
                if(module == null) return [];
                // AtkArrayDataHolder.StringArrays 是 StringArrayData**(AtkModule+0x1B90 內的 +0x30),
                // 取 [1] 會解參考這個雙重指標,所以指標本身與取回的項目都要判;
                // StringArrayCount 是總槽數,拿它擋掉越界索引。
                var holder = module->AtkArrayDataHolder;
                if(holder.StringArrays == null || holder.StringArrayCount <= 1) return [];
                var stringArray = holder.StringArrays[1];
                if(stringArray == null || stringArray->StringArray == null) return [];
                for(var i = 0; i < 16; i++)
                {
                    var str = stringArray->StringArray[i];
                    // 空指標與空字串一樣代表「後面沒有世界了」——原本會把 0 位址交給
                    // ReadStringNullTerminated 去掃,那是 AccessViolation 不是空字串。
                    if(str.Value == null) break;
                    var worldName = MemoryHelper.ReadStringNullTerminated((nint)str.Value).Trim();
                    if(worldName.IsNullOrEmpty()) break;
                    ret.Add(new(this, i, worldName));
                }
                return [.. ret];
            }
        }

        public override string AddonDescription { get; } = "World selection menu on login screen";

        public class World
        {
            public readonly _CharaSelectWorldServer Master;
            public readonly int Index;
            public readonly string Name;

            public World(_CharaSelectWorldServer master, int index, string name)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                Index = index;
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Master = master;
            }

            public void Select()
            {
                /*var evt = Master.CreateAtkEvent();
                var data = Master.CreateAtkEventData()
                    .Write<byte>(0x10, (byte)Index)
                    .Write<byte>(0x16, (byte)Index)
                    .Build();
                Master.Base->ReceiveEvent((AtkEventType)35, 0, &evt, &data);
                Master.Base->ReceiveEvent((AtkEventType)37, 0, &evt, &data);*/
                Callback.Fire(Master.Base, true, 25, 0, Index);
            }
        }
    }
}