<!-- ffxiv-tc-port 繁體中文說明開始 -->
# ECommons(台服 fork)

由 NightmareXIV 開發的插件共用函式庫，提供 IPC 封裝、原生 UI 讀取（`AtkReader`/
`AddonMaster`）、`TaskManager`、設定序列化等共用元件，是艦隊裡最多插件的基礎依賴。

## 台服 fork 的目的

跟隨艦隊釘 API13（本 repo 工作分支是 `pin-wrathcombo-tc-api13`），並對台服原生 UI 讀取路徑
做了大量判空加固、讓遊戲改版後特徵碼對不上時不至於拖垮整個插件，加上多個實機驗證出的真 bug
修正與記憶體用量改善。

## 與上游的差異

- **`AddonMasterImplementations`/`AtkReader` 大範圍補判空**：原生節點解參前一律先判空，
  失敗形式從 `AccessViolationException`（`try/catch` 攔不住）降級為可處理的錯誤。
- **遊戲改版後的容錯**：遊戲位址對不上時，玩家狀態查詢、任務佇列步驟、代名詞解析、介面元件
  判斷與多個視窗的按鈕讀取改成回報「不可用／讀不到」，不再每幀擲例外；過場自動跳過
  （`AutoCutsceneSkipper`）特徵碼對不上時只停用該功能、插件照常載入，改寫遊戲碼前後也會驗位元組。
- **地圖標記換算世界座標修正**：漏減地圖偏移，有偏移的地圖誤差達 89 碼。
- **`WKSLottery` 選輪盤修正**：對同一指標連送兩發 callback，第二發可能打在已關閉的視窗上。
- **`MJIHud.ManageMinions()` 修正**：誤按到隱居小屋管理鈕，呼叫者實際開錯視窗。
- **`ItemFinder.Close()` 與 `BannerList.UseAsInstantPortrait()` 修正**：兩者原本指到非按鈕
  節點，一直是空操作。
- **`EzIPC` 補上自訂具名委派支援**，並修掉 `LegacyTaskManager` 任務內呼叫 `Abort()` 的 NRE。
- **其他修正**：`FrameThrottler` 在過場動畫與隱藏 UI 期間不再停止計時（原本節流永不到期）；
  `IsComponentEnabled` 補上按住鈕與分頁元件的多載；新增 `EzConfig.Set`（匯入整份設定後存檔寫入
  匯入的那一份）；`ImGuiBB` 的顏色／字級標記沒成對時不再弄壞後面介面的樣式；
  `NeoTaskManager` 的 log 改印可辨識的任務名稱。
- **多處降低記憶體用量**：列舉下拉選單、職業與區域選擇器不再每幀重建選項；`Callback.Fire` 沒開
  詳細記錄時不組記錄字串；`ExcelWorldHelper` 不再每次建表；Splatoon 一次性標記少配置。

`AddonMasterImplementations` 目錄下有大量檔案因判空加固而改動，未逐一列出。

## 誰在用它

目前 25 個插件 repo 以子模組使用：`Artisan`、`AutoDuty`、`AutoHook`、`AutoRetainer`、`Avarice`、
`BOCCHI`（經 `Ocelot`）、`ChilledLeves`、`EurekaHelper`、`Explorers-Icebox`、`GatherBuddyReborn`、
`HoardFarm`、`ICE`、`LazyLoot`、`Lifestream`、`NecroLens`、`NotificationMaster`、`PalacePal`、
`Questionable`（含 `GatheringPathRenderer`）、`Saucy`、`SomethingNeedDoing`、`Splatoon`、`TextAdvance`、
`WrathCombo`、`YesAlready`、`visland`。

函式庫 `Ocelot`、`NightmareUI`、`PunishLib`、`ECommons.IPC`、`AutoRetainerAPI` 也建在它之上。

---

以下為上游原始 README，內容未經修改：

<!-- ffxiv-tc-port 繁體中文說明結束 -->

<section id="about">
<a href="#about" alt="About"><h1>About ECommons</h1></a>
  <p>ECommons is a multi-functional library designed to work within Dalamud Plugins. It features a variety of different systems and shortcuts which cuts out a lot of boiler plate code normally used to do standard plugin tasks.</p>
</section>

<section id="getting-started">
<a href="#getting-started" alt="Getting Started"><h2>Getting Started</h2></a>
Get ECommons from NuGet using a console command:

```
dotnet add package ECommons
```
Or simply find it via NuGet package manager GUI.
  
Then, initialize in the constructor of your plugin:

```
ECommonsMain.Init(pluginInterface, this);
```

where pluginInterface is a <b>DalamudPluginInterface</b>.

Don't forget to dispose it in your plugin's dispose method:
```
ECommonsMain.Dispose();
```

<section id="getting-started">
<a href="#getting-started" alt="Getting Started"><h2>v3 changes</h2></a>
To ensure consistent building experience, ECommons 3.0.0.0 and higher no longer reference Windows Forms in any way. Additionally, `RELEASEFORMS` and `DEBUGFORMS` versions were removed. If you have previously used `System.Windows.Forms.Keys` with ECommons, replace it with `ECommons.WindowsFormsReflector.Keys`. Internal copy/paste methods now use reflection to call Windows Forms.

<section id="using-modules">
<a href="#using-modules" alt="Using Modules"><h2>Using Modules</h3></a>
ECommons comes with various modules which needs to be initalised at plugin runtime. To do so, modify your initalising code as follows:

```
ECommonsMain.Init(pluginInterface, this, Modules.<Module>);
```

where \<Module> is one of the following:
- All (For all modules)
- SplatoonAPI
- DalamudReflector
- ObjectLife
- ObjectFunctions
</section>

---

> [!WARNING]
> As of [`2024-04-15`](https://github.com/NightmareXIV/ECommons/commit/b4be673) `TaskManager`'s namespace has changed.\
> Add `using ECommons.Automation.LegacyTaskManager;` as an immediate fix.

> [!WARNING]
> As of [`2024-04-14`](https://github.com/NightmareXIV/ECommons/commit/6f1fd30) Windows Forms and Windows Targeting are now disabled by default.\
> Manually set a build configuration with forms as a fix.
