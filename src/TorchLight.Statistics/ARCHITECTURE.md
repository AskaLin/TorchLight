# TorchLight.Statistics - 程式架構說明

## 重構後的架構

重構後的程式採用了清晰的職責分離設計，將原本龐大的 `Program.cs` 拆分為多個專注的類別。

## 核心類別說明

### 1. **Program.cs** (主程式)
- **職責**: 程式進入點，負責初始化和協調各組件
- **內容**: 
  - 初始化所有服務
  - 設定日誌檔案路徑
  - 啟動檔案監聽器
  - 處理程式生命週期

### 2. **GameLogProcessor** (遊戲日誌處理器)
- **職責**: 統籌所有日誌處理邏輯
- **功能**:
  - 協調各個管理器的工作
  - 處理不同類型的日誌行
  - 連接事件處理流程
- **依賴**:
  - `LineParser`: 解析日誌行
  - `BagInventoryManager`: 管理背包
  - `MapPickRecordManager`: 管理地圖記錄
  - `MapTransitionHandler`: 處理地圖切換
  - `ConsoleLogger`: 輸出日誌

### 3. **Services/BagInventoryManager** (背包庫存管理器)
- **職責**: 管理玩家背包的所有物品
- **功能**:
  - 初始化背包物品 (`InitializeBagItem`)
  - 更新物品數量 (`UpdateBagItem`)
  - 重置背包資料 (`Reset`)
  - 顯示背包內容 (`PrintInitializedBag`)
- **返回**: `ItemChangeResult` - 包含變更詳情

### 4. **Services/MapPickRecordManager** (地圖拾取記錄管理器)
- **職責**: 記錄在異界地圖中的所有拾取行為
- **功能**:
  - 開始記錄地圖 (`StartMapRecord`)
  - 結束記錄地圖 (`EndMapRecord`)
  - 記錄拾取物品 (`RecordPickedItem`)
  - 重置記錄 (`Reset`)
  - 顯示所有記錄 (`PrintAllRecords`)
- **返回**: `MapPickResult` - 包含拾取詳情
- **狀態追蹤**:
  - `IsInNetherrealmMap`: 是否在異界地圖
  - `CurrentMapName`: 當前地圖名稱

### 5. **Services/MapTransitionHandler** (地圖切換處理器)
- **職責**: 處理玩家在地圖間的移動
- **功能**:
  - 判斷地圖類型（藏身處/異界地圖）
  - 觸發記錄的開始和結束
  - 更新當前地圖狀態

### 6. **ConsoleLogger** (控制台日誌輸出器)
- **職責**: 格式化並輸出各種日誌訊息
- **功能**:
  - 記錄背包修改 (`LogBagModification`)
  - 記錄地圖拾取 (`LogMapPickItem`)
- **特色**: 統一管理所有控制台輸出，便於未來擴展（如寫入檔案、傳送到網路等）

## 資料流程

```
日誌檔案
    ↓
SafeFileTailWatcher (監聽新行)
    ↓
GameLogProcessor.ProcessLine()
    ├→ LineParser (解析)
    ├→ BagInventoryManager (更新背包)
    │    └→ ItemChangeResult
    ├→ MapPickRecordManager (記錄拾取)
    │  └→ MapPickResult
    ├→ MapTransitionHandler (處理切換)
    └→ ConsoleLogger (輸出結果)
```

## 優勢

### 1. **單一職責原則 (SRP)**
每個類別只負責一個明確的功能：
- `BagInventoryManager` → 只管理背包
- `MapPickRecordManager` → 只管理地圖記錄
- `ConsoleLogger` → 只負責輸出

### 2. **易於測試**
每個類別都可以獨立進行單元測試，不需要啟動整個程式。

### 3. **易於擴展**
- 想要更改輸出格式？只需修改 `ConsoleLogger`
- 想要支援新的事件類型？在 `GameLogProcessor` 中添加處理邏輯
- 想要保存記錄到資料庫？擴展 `MapPickRecordManager`

### 4. **可讀性高**
- `Program.cs` 現在只有約 50 行，一目了然
- 每個方法的職責清晰，命名明確
- 邏輯流程容易追蹤

### 5. **可維護性強**
- 修改某個功能時，只需關注對應的類別
- 減少程式碼重複
- 降低耦合度

## 與舊版本的對比

### 舊版 Program.cs
- **行數**: 約 200+ 行
- **方法數**: 3 個大方法包含所有邏輯
- **職責**: 混雜了所有功能
- **測試難度**: 很難單獨測試某個功能

### 新版架構
- **Program.cs**: 約 50 行（減少 75%）
- **類別數**: 6 個專注的類別
- **總行數**: 雖然檔案變多，但每個檔案都很短且易讀
- **測試難度**: 每個類別都可獨立測試

## 未來擴展方向

1. **資料持久化**: 新增 `DatabaseRepository` 儲存記錄
2. **UI 介面**: 建立 WPF 或 Avalonia 視窗應用程式
3. **即時通知**: 當撿到特定物品時發送通知
4. **統計分析**: 新增 `StatisticsAnalyzer` 分析拾取效率
5. **設定檔**: 允許使用者自訂監聽路徑和過濾規則
