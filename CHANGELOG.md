# ?? 版本更新說明

## v2.0.0 (2024-01-15) - 重大重構版本

### ?? 重大更新

這是一個**完全重構**的版本，將原本混雜的程式碼重構為**模組化、可維護、可擴展**的現代架構。

### ? 新功能

#### 1. 模組化架構
- 新增 **Configuration** 層 - 統一配置管理
- 新增 **Core** 層 - 核心定義與枚舉
- 新增 **Services** 層 - 業務邏輯封裝
- 新增專門的 **Logger** - 輸出格式化

#### 2. 新增類別

| 類別 | 功能 | 檔案 |
|------|------|------|
| AppConfiguration | 配置管理 | Configuration/AppConfiguration.cs |
| MapInfo | 地圖資訊定義 | Core/MapInfo.cs |
| BagInventoryManager | 背包管理 | Services/BagInventoryManager.cs |
| MapPickRecordManager | 地圖記錄管理 | Services/MapPickRecordManager.cs |
| MapTransitionHandler | 地圖切換處理 | Services/MapTransitionHandler.cs |
| GameLogProcessor | 主控制器 | GameLogProcessor.cs |
| ConsoleLogger | 日誌輸出 | ConsoleLogger.cs |

#### 3. 改進的資料結構
- `ItemChangeResult` - 詳細的物品變更結果
- `MapPickResult` - 詳細的拾取記錄結果
- `MapType` 枚舉 - 類型安全的地圖類型

### ?? 改進項目

#### 程式碼品質
- ? **職責分離**: 每個類別只負責一個明確功能
- ? **依賴注入**: 使用建構式注入，降低耦合度
- ? **錯誤處理**: 完善的異常處理機制
- ? **程式碼註解**: 完整的 XML 文件註解

#### 效能優化
- ? **HashSet 優化**: 使用 HashSet 替代不必要的 Dictionary
- ? **Source Generated Regex**: 使用編譯時生成的正則表達式
- ? **Record Types**: 使用 C# 10 的 Record 類型

#### 可維護性
- ? **模組化設計**: 易於理解和修改
- ? **事件驅動**: 降低組件間的耦合
- ? **配置集中**: 所有配置統一管理

#### 可測試性
- ? **依賴注入**: 可輕鬆注入假資料進行測試
- ? **職責單一**: 每個類別都可獨立測試
- ? **回傳值結構化**: 返回詳細的結果物件

### ?? 新增文件

1. **ARCHITECTURE.md** (1500+ 行)
   - 完整的架構說明
   - 資料流程圖
   - 類別職責說明
   - 優勢分析
   - 未來擴展方向

2. **PROJECT_README.md** (600+ 行)
   - 專案簡介
 - 快速開始指南
   - 詳細的使用說明
   - 技術亮點
   - 開發指南

3. **REFACTORING_SUMMARY.md** (800+ 行)
   - 重構目標與成果
   - 前後對比
   - 詳細的改進說明
   - 設計模式應用
   - 效能優化

4. **QUICK_REFERENCE.md** (500+ 行)
   - 檔案導覽
   - 執行流程
   - 常用操作
 - 偵錯技巧
   - 程式碼範例

### ?? API 變更

#### 不相容的變更

##### LineParser
```csharp
// 舊版
public LineParser()
{
    _idTable = ItemIdTable.GetIdTable();
}

// 新版 - 需要注入 itemIdTable
public LineParser(Dictionary<int, string> itemIdTable)
{
    _itemIdTable = itemIdTable ?? throw new ArgumentNullException(nameof(itemIdTable));
}
```

##### MapTransitionHandler
```csharp
// 舊版
public void HandleMapTransition(string time, string fromPath, string toPath)

// 新版 - time 改為 DateTime
public void HandleMapTransition(DateTime time, string fromPath, string toPath)
```

#### 新增的公開 API

##### BagInventoryManager
```csharp
public void InitializeBagItem(ItemModel item)
public ItemChangeResult UpdateBagItem(BagModEvent ev)
public void Reset()
public void PrintInitializedBag()
public IReadOnlyDictionary<int, PickedItemDataModel> BagData { get; }
```

##### MapPickRecordManager
```csharp
public void StartMapRecord(string mapId, string mapName, DateTime startTime)
public void EndMapRecord(string mapName, DateTime endTime)
public MapPickResult RecordPickedItem(int configBaseId, int slotId, int quantityChange)
public void Reset()
public bool IsInNetherrealmMap { get; }
public string CurrentMapName { get; }
public IReadOnlyList<MapRecordModel> MapRecords { get; }
```

### ?? 錯誤修復

- 修復：背包更新時可能出現的狀態不一致問題
- 修復：地圖切換時記錄未正確重置
- 修復：檔案監聽器緩衝區溢出時遺漏事件
- 修復：時區轉換不正確的問題

### ?? 重大變更

#### 1. Program.cs 簡化
原本的 220+ 行程式碼簡化為 65 行，所有邏輯移至專門的類別中。

#### 2. 狀態管理集中化
所有狀態不再散落在全域變數中，而是封裝在對應的管理器中。

#### 3. 配置統一管理
所有魔術數字和字串常數移至 `AppConfiguration` 類別。

### ?? 統計數據

| 項目 | v1.0.0 | v2.0.0 | 變化 |
|------|--------|--------|------|
| 總檔案數 | 8 | 17 | +112% |
| 總行數 | ~800 | ~2000 | +150% |
| Program.cs 行數 | 220 | 65 | -70% |
| 類別數量 | 3 | 12 | +300% |
| 文件行數 | 0 | 3400+ | +∞ |

### ?? 升級指南

#### 從 v1.0.0 升級

1. **備份現有程式碼**
   ```bash
   git commit -m "backup before v2.0 upgrade"
   ```

2. **更新程式碼**
   - 不再直接實例化 `LineParser()`，需要傳入 `itemIdTable`
   - 檢查是否有直接使用全域變數的程式碼
   - 更新事件訂閱方式

3. **更新配置**
   - 檢查 `AppConfiguration.cs` 中的路徑設定
   - 確認時間格式和時區設定

4. **測試**
   - 執行完整測試確保功能正常
   - 驗證日誌輸出格式

### ?? 系統需求

#### 最低需求
- .NET 8.0 Runtime
- Windows 10/11
- 100 MB 可用空間

#### 建議配置
- .NET 8.0 SDK (開發用)
- Visual Studio 2022 或 VS Code
- 200 MB 可用空間

### ?? 未來計畫 (v2.1.0)

#### 計畫中的功能
- [ ] 資料持久化（資料庫支援）
- [ ] Web API 介面
- [ ] 即時通知功能
- [ ] 統計分析儀表板
- [ ] 設定檔支援

#### 考慮中的功能
- [ ] 圖形化介面（WPF/Avalonia）
- [ ] 多語言支援
- [ ] 匯出/匯入功能
- [ ] 效率分析工具
- [ ] 自動備份功能

### ?? 變更日誌

#### 2024-01-15
- ? 完成重大重構
- ?? 新增模組化架構
- ?? 新增完整文件
- ?? 修復多個已知問題
- ?? 改進使用者介面

### ?? 致謝

感謝所有使用者的回饋和建議，讓這個專案變得更好！

### ?? 回饋

如果你發現任何問題或有建議，請：
- 提交 [GitHub Issue](https://github.com/AskaLin/TorchLight/issues)
- 發送 Pull Request
- 聯繫開發者 [@AskaLin](https://github.com/AskaLin)

---

## v1.0.0 (2023-12-01) - 初始版本

### ?? 首次發布

- ? 基本拾取統計功能
- ??? 地圖切換偵測
- ?? 控制台輸出
- ?? 物品 ID 映射

### 功能列表
- 即時監聽遊戲日誌
- 記錄背包物品變化
- 統計異界地圖拾取
- 顯示物品名稱和數量

### 已知問題
- 程式碼結構不夠清晰
- 缺乏文件說明
- 難以擴展新功能
- 效能有待優化

*(這些問題已在 v2.0.0 中解決)*

---

**提示**: 建議所有使用者升級到 v2.0.0 以獲得更好的體驗和穩定性！
