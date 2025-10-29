# ?? 火炬之光無限 - 拾取物品統計工具

> **Torchlight Infinite Item Tracker** - 即時監控遊戲拾取物品的專業工具

## ?? 專案簡介

這是一個用於《火炬之光：無限》的拾取物品統計工具，透過分析遊戲日誌檔案，自動記錄玩家在異界地圖中的所有拾取行為，並提供詳細的統計資訊。

### 主要功能

? **即時監控** - 持續監聽遊戲日誌，即時追蹤物品變化  
? **背包管理** - 完整追蹤背包中所有物品的數量變化  
? **地圖記錄** - 自動識別異界地圖，記錄每張地圖的拾取統計  
? **物品識別** - 內建 300+ 種物品的中文名稱  
? **詳細日誌** - 提供時間、數量、欄位等完整資訊  

## ?? 快速開始

### 系統需求
- Windows 10/11
- .NET 8.0 Runtime
- 火炬之光：無限遊戲

### 執行步驟
1. 安裝 [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 啟動《火炬之光：無限》遊戲
3. 執行 `TorchLight.Statistics.exe`
4. 進入異界地圖開始刷圖

## ?? 使用流程

```
啟動程式 → 載入物品定義 → 開始監聽日誌
   ↓
       進入異界地圖（自動開始記錄）
      ↓
       拾取物品（即時顯示統計）
↓
      離開地圖（顯示完整報表）
```

## ?? 專案架構

本專案採用**模組化設計**，職責清晰分離：

### 核心組件

| 組件 | 職責 | 位置 |
|------|------|------|
| **GameLogProcessor** | 主控制器，統籌所有處理邏輯 | `GameLogProcessor.cs` |
| **BagInventoryManager** | 管理背包庫存 | `Services/` |
| **MapPickRecordManager** | 記錄地圖拾取 | `Services/` |
| **MapTransitionHandler** | 處理地圖切換 | `Services/` |
| **LineParser** | 解析日誌行 | `LineParser.cs` |
| **ItemChangeBlockProcessor** | 處理物品變更區塊 | `LogEvent.cs` |
| **SafeFileTailWatcher** | 監聽檔案變化 | `SafeFileTailWatcher.cs` |

### 資料流程

```
日誌檔案 (UE_game.log)
    ↓ 
SafeFileTailWatcher (檔案監聽)
    ↓
GameLogProcessor (主處理器)
    ├→ LineParser (解析)
    ├→ BagInventoryManager (背包管理)
    ├→ MapPickRecordManager (地圖記錄)
    └→ ConsoleLogger (輸出)
```

## ?? 技術亮點

### 1. 雙重檔案監聽機制
- **FileSystemWatcher**: 即時監聽檔案變化
- **輪詢機制**: 定期檢查，防止遺漏事件
- **Debounce**: 防止短時間多次觸發

### 2. 區塊處理機制
使用 `Dictionary<ThreadId, Context>` 追蹤每個執行緒的區塊狀態，支援：
- 即時模式（立即處理）
- 彙整模式（批次處理）
- 超時保護

### 3. 正則表達式優化
使用 .NET 8 的 **Source Generator** 特性：
```csharp
[GeneratedRegex(@"pattern", RegexOptions.Singleline)]
public static partial Regex BagItemLine();
```
? 編譯時生成，效能更佳  
? 避免執行時編譯開銷  

### 4. Record Types
使用 C# 10 的 Record 定義事件：
```csharp
public record BagModEvent(
    DateTime Time,
    int ThreadId,
    int ConfigBaseId,
 int Num,
    string Action
) : LogEvent(Time, ThreadId);
```
? 不可變性（Immutable）  
? 值相等比較  
? 簡潔語法  

### 5. 事件驅動架構
```csharp
public event Action<BagModEvent>? OnBagModInsideBlock;
```
? 降低耦合度  
? 支援多訂閱者  
? 易於擴展  

## ?? 專案結構

```
TorchLight.Statistics/
├── Configuration/   # 配置管理
│   └── AppConfiguration.cs
├── Core/           # 核心定義
│└── MapInfo.cs
├── Models/                 # 資料模型
│   ├── ItemModel.cs
│   ├── PickedItemDataModel.cs
│   └── MapRecordModel.cs
├── Services/   # 業務服務
│ ├── BagInventoryManager.cs
│   ├── MapPickRecordManager.cs
│   └── MapTransitionHandler.cs
├── GameLogProcessor.cs     # 主處理器
├── LineParser.cs     # 日誌解析
├── LogEvent.cs# 事件定義
├── MapMapper.cs # 地圖映射
├── ItemIdTable.cs          # 物品表處理
├── SafeFileTailWatcher.cs  # 檔案監聽
├── ConsoleLogger.cs        # 日誌輸出
└── Program.cs  # 程式入口
```

## ?? 設計原則

### SOLID 原則
- ? **單一職責 (SRP)**: 每個類別只負責一個功能
- ? **開放封閉 (OCP)**: 易於擴展，無需修改現有程式碼
- ? **依賴反轉 (DIP)**: 依賴抽象而非具體實作

### 設計模式
- **觀察者模式 (Observer)**: 事件驅動架構
- **策略模式 (Strategy)**: 不同的日誌處理策略
- **單例模式 (Singleton)**: 配置管理

## ?? 開發指南

### 建置專案
```bash
dotnet restore
dotnet build
dotnet run --project src/TorchLight.Statistics
```

### 新增物品定義
編輯 `ItemIdTable.json`:
```json
{
  "物品ID": {
    "name": "物品名稱",
    "type": "物品類型"
  }
}
```

### 新增地圖支援
在 `MapMapper.cs` 中添加：
```csharp
private static readonly Dictionary<string, string> _mapNameMapping = new()
{
 { "地圖ID", "地圖名稱" }
};
```

## ?? 未來計畫

- [ ] 資料持久化（資料庫/檔案）
- [ ] Web API 介面
- [ ] 即時通知功能
- [ ] 圖形化介面（WPF/Avalonia）
- [ ] 統計分析功能
- [ ] 多語言支援

## ? 常見問題

**Q: 找不到日誌檔案？**  
A: 修改 `AppConfiguration.cs` 中的 `CandidateLogPaths`

**Q: 物品顯示「未知物品」？**  
A: 該物品ID尚未加入 `ItemIdTable.json`，可手動添加

**Q: 會影響遊戲效能嗎？**  
A: 不會，程式只讀取日誌檔案，不影響遊戲

## ?? 更新日誌

### v2.0.0 (2024-01)
- ? 完全重構專案架構
- ?? 模組化設計
- ?? 改進輸出格式
- ?? 修復已知問題
- ?? 完整文件

## ?? 授權

MIT License

## ?? 貢獻

歡迎提交 Issue 和 Pull Request！

## ?? 聯絡

- GitHub: [@AskaLin](https://github.com/AskaLin)
- 專案: [TorchLight](https://github.com/AskaLin/TorchLight)

---

**?? 提示**: 如果這個工具對你有幫助，請給個星星 ?！

更多詳細文件請參閱 [ARCHITECTURE.md](src/TorchLight.Statistics/ARCHITECTURE.md)
