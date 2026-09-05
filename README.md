<!-- ffxiv-tc-port 繁體中文說明開始 -->
# ECommons(台服 fork)

由 NightmareXIV 開發的插件共用函式庫，提供 IPC 封裝、原生 UI 讀取（`AtkReader`/
`AddonMaster`）、`TaskManager`、設定序列化等共用元件，是艦隊裡最多插件的基礎依賴。

## 台服 fork 的目的

跟隨艦隊釘 API13（本 repo 工作分支是 `pin-wrathcombo-tc-api13`），並對台服原生 UI 讀取路徑
做了大量判空加固，加上多個實機驗證出的真 bug 修正：

- **`AddonMasterImplementations`/`AtkReader` 大範圍補判空**：原生節點解參前一律先判空，
  失敗形式從 `AccessViolationException`（`try/catch` 攔不住）降級為可處理的錯誤。
- **地圖標記換算世界座標修正**：漏減地圖偏移，有偏移的地圖誤差達 89 碼。
- **`WKSLottery` 選輪盤修正**：對同一指標連送兩發 callback，第二發可能打在已關閉的視窗上。
- **`MJIHud.ManageMinions()` 修正**：誤按到隱居小屋管理鈕，呼叫者實際開錯視窗。
- **`ItemFinder.Close()` 與 `BannerList.UseAsInstantPortrait()` 修正**：兩者原本指到非按鈕
  節點，一直是空操作。
- **`EzIPC` 補上自訂具名委派支援**，並修掉 `LegacyTaskManager` 任務內呼叫 `Abort()` 的 NRE。

## 與上游的差異

以上各項為主；`AddonMasterImplementations` 目錄下有大量檔案因判空加固而改動，未逐一列出。

## 誰在用它

艦隊裡 24 個插件消費：`Artisan`、`AutoDuty`、`AutoHook`、`AutoRetainer`、`Avarice`、`BOCCHI`、
`ChilledLeves`、`EurekaHelper`、`Explorers-Icebox`、`GatherBuddyReborn`、`ICE`、`LazyLoot`、
`Lifestream`、`NecroLens`、`NotificationMaster`、`PalacePal`、`Questionable`、`Saucy`、
`SomethingNeedDoing`、`Splatoon`、`TextAdvance`、`WrathCombo`、`YesAlready`、`visland`。

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
