# LogProcessor 架構重構說明

## 📋 概述

本次重構將原本分散的 LogProcessor 整合為統一的架構，採用**責任鏈模式（Chain of Responsibility）**和**模板方法模式（Template Method）**，提高程式碼的可維護性和可擴展性。

## 🏗️ 架構設計

### 類別關係圖

```
BaseLogProcessor (抽象基類)
    ├─ InitBagProcessor (背包初始化)
    ├─ PickedItemProcessor (拾取物品)
    ├─ OpenMapProcessor (開啟地圖)
    └─ OpenSeasonMapProcessor (開啟賽季地圖)

GameLogProcessor (主處理器)
    └─ 持有並管理所有 BaseLogProcessor
```

## 🎯 核心概念

### 1. BaseLogProcessor（基類）

**職責**：提供統一的區塊處理邏輯框架

**核心方法**：
- `HandleLine(string line)` - 處理單行日誌（模板方法）
- `IsBlockStart(string line)` - 判斷區塊開始（抽象方法）
- `IsBlockEnd(string line)` - 判斷區塊結束（抽象方法）
- `OnBlockStart(string line)` - 區塊開始處理（抽象方法）
- `OnBlockEnd(string line)` - 區塊結束處理（抽象方法）
- `ProcessBlockLine(string line)` - 處理區塊內的行（抽象方法）
- `Reset()` - 重置處理器狀態（虛方法）

**處理流程**：
```
1. 檢查是否為區塊開始
   ├─ 是 → 設定 IsInBlock = true，呼叫 OnBlockStart()
   └─ 否 → 繼續

2. 如果 IsInBlock == true
   ├─ 檢查是否為區塊結束
   │  ├─ 是 → 呼叫 OnBlockEnd()，設定 IsInBlock = false
   │  └─ 否 → 呼叫 ProcessBlockLine()
   └─ 返回 true (已處理)

3. 返回 false (未處理)
```

### 2. GameLogProcessor（責任鏈）

**職責**：協調所有處理器，管理全域事件

**處理器鏈（優先級由高到低）**：
1. `InitBagProcessor` - 背包初始化（最高優先級）
2. `PickedItemProcessor` - 拾取物品
3. `OpenMapProcessor` - 開啟地圖
4. `OpenSeasonMapProcessor` - 開啟賽季地圖

**處理流程**：
```csharp
public void ProcessLine(string line)
{
    // 1. 責任鏈處理
    foreach (var processor in _processorChain)
    {
        if (processor.HandleLine(line))
        {
            return; // 已處理，結束
        }
    }

    // 2. 處理全域事件
    ProcessGlobalEvents(line);
}
```

## 📦 各處理器說明

### InitBagProcessor（背包初始化）

**觸發條件**：
- 開始：`BagMgr@:InitBagData` 且不是忽略的 PageId
- 結束：非初始化行（狀態機檢測）

**事件**：
- `OnInitStarted` - 初始化開始
- `OnItemInitialized` - 單個物品初始化（即時模式）
- `OnInitCompleted` - 初始化完成（批次模式）

**範例日誌**：
```
BagMgr@:InitBagData PageId = 102 SlotId = 1 ConfigBaseId = 5001 Num = 100
BagMgr@:InitBagData PageId = 102 SlotId = 2 ConfigBaseId = 5002 Num = 50
...
```

---

### PickedItemProcessor（拾取物品）

**觸發條件**：
- 開始：`ItemChange@ ProtoName=PickItems start` 或 `PickItem start`
- 結束：`ItemChange@ ProtoName=PickItems end` 或 `PickItem end`

**事件**：
- `OnItemsPicked` - 拾取物品

**範例日誌**：
```
ItemChange@ ProtoName=PickItems start
BagMgr@:Modfy BagItem PageId = 102 SlotId = 5 ConfigBaseId = 5001 Num = 105
ItemChange@ ProtoName=PickItems end
```

---

### OpenMapProcessor（開啟地圖）

**觸發條件**：
- 開始：`ItemChange@ ProtoName=Spv3Open start`
- 結束：`[Game] UGameMgr::EnterLevel`

**事件**：
- `OnMapStart` - 開圖開始
- `OnMapComplete` - 開圖完成
- `OnItemChangeInMapBlock` - 區塊內的物品變更（開圖材料）

**範例日誌**：
```
ItemChange@ ProtoName=Spv3Open start
+AreaUniqueId [1234567890]
+mapId [1061000]
BagMgr@:Modfy BagItem PageId = 102 SlotId = 1 ConfigBaseId = 3001 Num = 95
[Game] UGameMgr::EnterLevel
```

---

### OpenSeasonMapProcessor（開啟賽季地圖）

**觸發條件**：
- 開始：`PageApplyBase@ EnterScene ScenePath = World'/Game/Art/Season/`
- 結束：`[Game] UGameMgr::EnterLevel`

**事件**：
- `OnMapStart` - 賽季地圖開始
- `OnMapComplete` - 賽季地圖完成
- `OnItemChangeInMapBlock` - 區塊內的物品變更

**範例日誌**：
```
PageApplyBase@ EnterScene ScenePath = World'/Game/Art/Season/Map01.Map01'
+AreaUniqueId [9876543210]
+mapId [2001000]
[Game] UGameMgr::EnterLevel
```

## 🔄 重構前後對比

### 重構前

**問題**：
- ❌ 每個處理器獨立實現相似邏輯
- ❌ 區塊狀態管理分散
- ❌ 難以新增處理器
- ❌ 處理順序不明確

**程式碼片段**：
```csharp
// OpenMapProcessor.cs
private bool _inOpenMapBlock = false;

public bool HandleLine(string line)
{
    if (line.Contains("ItemChange@ ProtoName=Spv3Open start"))
    {
        _inOpenMapBlock = true;
        // ...
        return true;
    }
    else if (_inOpenMapBlock)
    {
        if (line.Contains("[Game] UGameMgr::EnterLevel"))
        {
            _inOpenMapBlock = false;
        }
        // ...
        return true;
    }
    return false;
}
```

### 重構後

**優點**：
- ✅ 統一的基類封裝共同邏輯
- ✅ 狀態管理集中在基類
- ✅ 新增處理器只需實現抽象方法
- ✅ 責任鏈清晰定義優先級

**程式碼片段**：
```csharp
// OpenMapProcessor.cs
public class OpenMapProcessor : BaseLogProcessor
{
    protected override bool IsBlockStart(string line)
    {
        return LineParser.GetLineDateTime(line, "ItemChange@ ProtoName=Spv3Open start", out _);
    }

    protected override bool IsBlockEnd(string line)
    {
        return line.Contains("[Game] UGameMgr::EnterLevel");
    }

    protected override void OnBlockStart(string line) { /* ... */ }
    protected override void OnBlockEnd(string line) { /* ... */ }
    protected override void ProcessBlockLine(string line) { /* ... */ }
}
```

## 📈 擴展性

### 新增處理器步驟

1. **繼承 BaseLogProcessor**
```csharp
public class NewFeatureProcessor : BaseLogProcessor
{
    // 實現抽象方法
}
```

2. **實現抽象方法**
```csharp
protected override bool IsBlockStart(string line) => /* 判斷邏輯 */;
protected override bool IsBlockEnd(string line) => /* 判斷邏輯 */;
protected override void OnBlockStart(string line) { /* 開始處理 */ }
protected override void OnBlockEnd(string line) { /* 結束處理 */ }
protected override void ProcessBlockLine(string line) { /* 行處理 */ }
```

3. **註冊到 GameLogProcessor**
```csharp
// GameLogProcessor.cs
private readonly NewFeatureProcessor _newFeatureProcessor;

public GameLogProcessor(WebViewHub webViewHub = null)
{
    _newFeatureProcessor = new NewFeatureProcessor();
    
    // 加入處理器鏈
    _processorChain.Add(_newFeatureProcessor);
    
    // 註冊事件
    _newFeatureProcessor.OnSomeEvent += HandleSomeEvent;
}
```

## 🎓 設計模式應用

### 1. 模板方法模式（Template Method）

**位置**：`BaseLogProcessor.HandleLine()`

**目的**：定義算法骨架，延遲部分步驟到子類

**好處**：
- 避免重複程式碼
- 統一處理流程
- 易於維護

---

### 2. 責任鏈模式（Chain of Responsibility）

**位置**：`GameLogProcessor._processorChain`

**目的**：將請求沿著處理器鏈傳遞，直到某個處理器處理它

**好處**：
- 解耦發送者和接收者
- 動態組合處理器
- 靈活的優先級控制

---

### 3. 觀察者模式（Observer）

**位置**：各處理器的事件（`event Action`）

**目的**：當狀態改變時，自動通知所有觀察者

**好處**：
- 低耦合
- 可擴展性高
- 易於維護

## 🧪 測試建議

### 單元測試

```csharp
[Test]
public void InitBagProcessor_ShouldHandleInitBlock()
{
    var processor = new InitBagProcessor();
    var itemsInitialized = new List<ItemModel>();

    processor.OnItemInitialized += itemsInitialized.Add;

    processor.HandleLine("BagMgr@:InitBagData PageId = 102 SlotId = 1 ConfigBaseId = 5001 Num = 100");
    processor.HandleLine("BagMgr@:InitBagData PageId = 102 SlotId = 2 ConfigBaseId = 5002 Num = 50");
    processor.HandleLine("Some other line"); // 結束

    Assert.AreEqual(2, itemsInitialized.Count);
}
```

### 整合測試

```csharp
[Test]
public void GameLogProcessor_ShouldProcessPickupCorrectly()
{
    var processor = new GameLogProcessor();
    var pickupEvents = new List<ItemChangeEvent>();

    // 模擬日誌序列
    processor.ProcessLine("ItemChange@ ProtoName=PickItems start");
    processor.ProcessLine("BagMgr@:Modfy BagItem PageId = 102 SlotId = 1 ConfigBaseId = 5001 Num = 105");
    processor.ProcessLine("ItemChange@ ProtoName=PickItems end");

    // 驗證結果
    Assert.IsTrue(processor.BagInventoryManager.BagData.ContainsKey(5001));
}
```

## 📚 相關文件

- [LineParser.cs](../LineParser.cs) - 日誌解析工具
- [BagInventoryManager.cs](../Services/BagInventoryManager.cs) - 背包管理
- [MapPickRecordManager.cs](../Services/MapPickRecordManager.cs) - 地圖記錄管理

## 🔗 參考資源

- [責任鏈模式 - Refactoring Guru](https://refactoring.guru/design-patterns/chain-of-responsibility)
- [模板方法模式 - Refactoring Guru](https://refactoring.guru/design-patterns/template-method)
- [觀察者模式 - Refactoring Guru](https://refactoring.guru/design-patterns/observer)

---

**最後更新**：2024-01-XX
**版本**：2.0
**作者**：開發團隊
