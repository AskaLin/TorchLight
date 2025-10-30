# ?? 快速參考指南

## ?? 檔案導覽

### ?? 核心檔案
- **Program.cs** - 程式進入點
  - 初始化所有組件
  - 啟動檔案監聽器
  - 處理程式生命週期

- **GameLogProcessor.cs** - 主控制器
  - 統籌所有日誌處理
  - 協調各個管理器
  - 事件分發

### ?? 服務層 (Services/)
- **BagInventoryManager.cs** - 背包管理
  - `InitializeBagItem()` - 初始化背包物品
  - `UpdateBagItem()` - 更新物品數量
  - `Reset()` - 重置背包

- **MapPickRecordManager.cs** - 地圖記錄
  - `StartMapRecord()` - 開始記錄地圖
  - `RecordPickedItem()` - 記錄拾取
  - `EndMapRecord()` - 結束記錄

- **MapTransitionHandler.cs** - 地圖切換
  - `HandleMapTransition()` - 處理地圖切換

### ?? 解析層
- **LineParser.cs** - 日誌行解析
  - `IsLoginStart()` - 判斷登入
  - `IsInitFinished()` - 判斷初始化完成
  - `IsMoveMap()` - 判斷地圖切換
  - `GetItemData()` - 解析物品資料

- **LogEvent.cs** - 事件定義與處理
  - `ItemChangeBlockProcessor` - 區塊處理器
  - `BagModEvent` - 背包修改事件

### ??? 工具層
- **MapMapper.cs** - 地圖映射
  - `GetMapInfo()` - 獲取地圖資訊
  - `IsHideoutMap()` - 判斷藏身處
  - `IsNetherrealmMap()` - 判斷異界地圖

- **ItemIdTable.cs** - 物品ID表
  - `GetIdTable()` - 載入物品定義

- **SafeFileTailWatcher.cs** - 檔案監聽
  - 雙重機制（監聽 + 輪詢）
  - 防抖動處理

- **ConsoleLogger.cs** - 日誌輸出
  - `LogBagModification()` - 記錄背包修改
  - `LogMapPickItem()` - 記錄地圖拾取

### ?? 配置層
- **Configuration/AppConfiguration.cs**
  - 所有配置常數
  - 日誌路徑
  - 時間格式

## ?? 資料模型 (Models/)
- **ItemModel** - 物品基本資訊
- **PickedItemDataModel** - 拾取物品詳細資料
- **MapRecordModel** - 地圖記錄

## ?? 執行流程

### 啟動階段
```
Program.cs
  → 載入 ItemIdTable
  → 初始化 LineParser
  → 初始化 ItemChangeBlockProcessor
  → 建立 GameLogProcessor
  → 啟動 SafeFileTailWatcher
```

### 運行階段
```
檔案變化
  → SafeFileTailWatcher 偵測
  → 觸發 OnNewLine 事件
  → GameLogProcessor.ProcessLine()
    ├→ 判斷日誌類型
    ├→ 呼叫對應的處理器
    └→ 輸出結果
```

## ?? 常用操作

### 新增物品定義
1. 開啟 `ItemIdTable.json`
2. 添加物品：
```json
{
  "123456": {
    "name": "新物品名稱",
    "type": "物品類型"
  }
}
```

### 新增地圖支援
1. 開啟 `MapMapper.cs`
2. 添加到對應的集合：
```csharp
private static readonly Dictionary<string, string> _mapNameMapping = new()
{
    { "新地圖ID", "新地圖名稱" }
};

private static readonly HashSet<string> _netherrealmMapIds =
[
    "新地圖ID"  // 如果是異界地圖
];
```

### 自訂日誌路徑
1. 開啟 `Configuration/AppConfiguration.cs`
2. 修改 `CandidateLogPaths`：
```csharp
public static readonly string[] CandidateLogPaths =
[
    @"你的自訂路徑\UE_game.log"
];
```

### 調整監聽參數
在 `AppConfiguration.cs` 中修改：
```csharp
public const int FileWatcherDebounceMs = 500;  // 防抖動時間
public const int FilePollingIntervalSeconds = 2;  // 輪詢間隔
```

## ?? 偵錯技巧

### 檢查日誌解析
在 `LineParser.cs` 的各方法中添加中斷點：
- `IsInitBagItemData()`
- `GetItemData()`
- `GetMapPathData()`

### 追蹤背包變化
在 `BagInventoryManager.cs` 中：
- `UpdateBagItem()` - 查看物品更新邏輯
- `ItemChangeResult` - 檢查返回值

### 追蹤地圖記錄
在 `MapPickRecordManager.cs` 中：
- `RecordPickedItem()` - 查看記錄邏輯
- `MapPickResult` - 檢查返回值

### 檢查事件觸發
在 `ItemChangeBlockProcessor.cs` 中：
- `HandleLine()` - 查看區塊處理
- `OnBagModInsideBlock` - 確認事件觸發

## ?? 程式碼範例

### 讀取背包資料
```csharp
var bagData = _bagInventoryManager.BagData;
foreach (var item in bagData)
{
    Console.WriteLine($"{item.Value.Name}: {item.Value.Total}");
}
```

### 獲取地圖記錄
```csharp
var records = _mapPickRecordManager.MapRecords;
foreach (var record in records)
{
    Console.WriteLine($"地圖: {record.Name}, 時間: {record.UseTime}");
    foreach (var item in record.PickRecord)
    {
        Console.WriteLine($"  {item.Value.Name}: {item.Value.Total}");
    }
}
```

### 訂閱事件
```csharp
// 訂閱區塊開始事件
_itemChangeProcessor.OnBlockStarted += (ev) =>
{
    Console.WriteLine($"區塊開始: {ev.ProtoName}");
};

// 訂閱背包修改事件
_itemChangeProcessor.OnBagModInsideBlock += (ev) =>
{
    Console.WriteLine($"物品修改: {ev.ConfigBaseId}, 數量: {ev.Num}");
};
```

## ?? 測試建議

### 單元測試範例
```csharp
[Fact]
public void BagInventoryManager_UpdateBagItem_ShouldCalculateCorrectly()
{
    // Arrange
    var itemTable = new Dictionary<int, string> { { 1001, "測試物品" } };
    var manager = new BagInventoryManager(itemTable);
    
    // Act
    var result = manager.UpdateBagItem(new BagModEvent(
        DateTime.Now, 1, 100, 1, 1001, 10, "PickItems", "Modfy BagItem"
    ));
    
    // Assert
    Assert.Equal(10, result.NewTotalCount);
    Assert.Equal(10, result.QuantityChange);
}
```

### 整合測試
1. 準備測試日誌檔案
2. 執行程式指向測試檔案
3. 驗證輸出結果

## ?? 相關文件

- **ARCHITECTURE.md** - 詳細架構說明
- **PROJECT_README.md** - 完整專案文件
- **REFACTORING_SUMMARY.md** - 重構總結

## ?? 最佳實踐

### 程式碼風格
- ? 使用有意義的變數名稱
- ? 每個方法只做一件事
- ? 添加 XML 文件註解
- ? 處理異常情況

### 效能考量
- ? 使用 HashSet 而非 Dictionary（當值不重要時）
- ? 使用 Source Generated Regex
- ? 避免不必要的字串操作

### 維護性
- ? 保持類別職責單一
- ? 使用依賴注入
- ? 事件驅動架構
- ? 完整的錯誤處理

## ?? 疑難排解

### 問題：找不到日誌檔案
**解決**：檢查 `AppConfiguration.CandidateLogPaths`

### 問題：物品顯示「未知物品」
**解決**：檢查 `ItemIdTable.json` 是否包含該物品ID

### 問題：地圖不被識別
**解決**：在 `MapMapper.cs` 中添加地圖定義

### 問題：事件沒有觸發
**解決**：檢查 `ItemChangeBlockProcessor` 的 `_targetProtocols`

## ?? 獲取幫助

- 查看 [GitHub Issues](https://github.com/AskaLin/TorchLight/issues)
- 閱讀完整文件
- 檢查程式碼註解

---

**提示**: 這份指南涵蓋了最常用的操作和概念，更詳細的資訊請參閱各個文件！
