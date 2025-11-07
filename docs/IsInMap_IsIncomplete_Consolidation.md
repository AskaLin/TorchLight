# IsInMap 與 IsIncomplete 狀態整合說明

## 概述

本次整合統一了 `MapPickRecordManager`、`MapRecordViewModel` 和 `MapRecordModel` 中 `IsInMap` 和 `IsIncomplete` 的使用邏輯。

---

## 問題分析

### 原有問題

1. **狀態重複**: `MapRecordViewModel` 複製了 `MapPickRecordManager` 的狀態
2. **邏輯混亂**: `SetIsInMap(bool)` 方法總是設定 `IsIncomplete = true`，語意不清
3. **用途不明**: `IsIncomplete` 的使用時機和條件不明確

---

## 改進方案

### 1. MapPickRecordManager（服務層）- 狀態來源

**IsInMap（是否在地圖中）**
- `private set` - 只能內部修改
- `true`: 玩家正常進入地圖並開始記錄（`StartMapRecord` 時設定）
- `false`: 不在地圖中或已離開（`EndMapRecord` 時設定）

**IsIncomplete（是否有未完成的地圖）**
- `private set` - 只能內部修改
- `true`: 地圖記錄因異常結束（遊戲關閉、斷線等）而未完成
- `false`: 正常狀態

### 2. 新增方法

#### ✅ `MarkCurrentMapAsIncomplete()`
```csharp
/// <summary>
/// 標記當前地圖為未完成狀態 (用於遊戲異常關閉、斷線等情況)
/// </summary>
public void MarkCurrentMapAsIncomplete()
{
    if (IsInMap && _currentMapRecord.StartTime != DateTime.MinValue)
    {
        IsIncomplete = true;
        Log.Warning("當前地圖標記為未完成狀態: {MapName}({Token})", 
                    _currentMapRecord.Name, _currentMapRecord.RecordId);
    }
}
```

**使用時機**:
- 遊戲異常關閉時（`LineParser.CloseGame()`）
- 斷線時
- 其他異常情況導致玩家未能正常離開地圖

#### ❌ 移除 `SetIsInMap(bool)`
原有方法邏輯混亂，已移除：
```csharp
// ❌ 已移除
public void SetIsInMap(bool isInMap)
{
    IsInMap = isInMap;
    IsIncomplete = true; // 總是設為 true，不合理
}
```

---

## 狀態流程

### 正常流程
```
1. StartMapRecord()
   ├─ IsInMap = true
   └─ IsIncomplete = false

2. (玩家進行遊戲)

3. EndMapRecord()
   ├─ IsInMap = false
   └─ IsIncomplete = false
```

### 異常流程（遊戲關閉）
```
1. StartMapRecord()
   ├─ IsInMap = true
   └─ IsIncomplete = false

2. (玩家進行遊戲)

3. LineParser.CloseGame() 偵測到
   └─ MarkCurrentMapAsIncomplete()
      ├─ IsInMap = true (保持)
      └─ IsIncomplete = true

4. (應用程式可選擇是否自動結算或保留未完成狀態)
```

---

## 類別職責劃分

### MapPickRecordManager（服務層）
- **職責**: 狀態的唯一來源（Single Source of Truth）
- **屬性**:
  - `IsInMap` - 管理當前是否在地圖中
  - `IsIncomplete` - 管理地圖是否未完成
- **方法**:
  - `StartMapRecord()` - 開始記錄，設定 `IsInMap = true, IsIncomplete = false`
  - `EndMapRecord()` - 結束記錄，設定 `IsInMap = false, IsIncomplete = false`
  - `MarkCurrentMapAsIncomplete()` - 標記未完成，設定 `IsIncomplete = true`

### MapRecordViewModel（視圖模型）
- **職責**: 從 `MapPickRecordManager` 同步狀態並傳遞給前端
- **屬性**:
  - `IsInMap` - 從 `MapPickRecordManager.IsInMap` 同步
  - `IsIncomplete` - 從 `MapPickRecordManager.IsIncomplete` 同步
- **說明**: 這些屬性僅用於傳遞給前端顯示，不應在此層級修改

### MapRecordModel（資料模型）
- **職責**: 儲存已完成的地圖記錄
- **屬性**: 不包含 `IsInMap` 和 `IsIncomplete`（因為已完成的記錄不需要這些狀態）

---

## GameLogProcessor 變更

### 原有程式碼
```csharp
// 回到避難所
if (line.Contains("[Game] UGameMgr::EnterLevel(110) mode=1 reload=0."))
{
    _mapPickRecordManager.ReturnTime = LineParser.GetLineDateTime(line);
    Log.Information($"偵測到返回避難所, 紀錄返回時間 {_mapPickRecordManager.ReturnTime:HH:mm:ss.fff}");
    _mapPickRecordManager.SetIsInMap(false); // ❌ 有問題：總是設定 IsIncomplete = true
    NotifyCurrentMapUpdate();
}

// 遊戲關閉
if (LineParser.CloseGame(line))
{
    Log.Information("偵測到遊戲關閉, 結算關卡資料");
    NotifyNewMapRecord();
}
```

### 改進後的程式碼
```csharp
// 回到避難所
if (line.Contains("[Game] UGameMgr::EnterLevel(110) mode=1 reload=0."))
{
    _mapPickRecordManager.ReturnTime = LineParser.GetLineDateTime(line);
    Log.Information($"偵測到返回避難所, 紀錄返回時間 {_mapPickRecordManager.ReturnTime:HH:mm:ss.fff}");
    // ✅ 不需要調用 SetIsInMap，EndMapRecord 時會自動設定 IsInMap = false
    NotifyCurrentMapUpdate();
}

// 遊戲關閉 - 標記當前地圖為未完成
if (LineParser.CloseGame(line))
{
    Log.Information("偵測到遊戲關閉");
    if (_mapPickRecordManager.IsInMap)
    {
        _mapPickRecordManager.MarkCurrentMapAsIncomplete(); // ✅ 明確標記為未完成
    }
    NotifyNewMapRecord();
}
```

---

## 使用範例

### 正常使用
```csharp
// 開始地圖記錄
_mapPickRecordManager.StartMapRecord(DateTime.Now);
// IsInMap = true, IsIncomplete = false

// (玩家進行遊戲)

// 正常結束地圖
_mapPickRecordManager.EndMapRecord(DateTime.Now);
// IsInMap = false, IsIncomplete = false
```

### 異常處理
```csharp
// 偵測到遊戲關閉
if (LineParser.CloseGame(line))
{
    if (_mapPickRecordManager.IsInMap)
    {
        // 標記為未完成
        _mapPickRecordManager.MarkCurrentMapAsIncomplete();
        // IsInMap = true, IsIncomplete = true
    }
}

// 後續可以選擇：
// 1. 自動結算未完成的地圖
// 2. 保留未完成狀態，等待玩家重新登入後決定如何處理
```

---

## 前端影響

### GetCurrentMapData() 變更
```csharp
public MapRecordViewModel GetCurrentMapData()
{
    var currentRecord = _mapPickRecordManager.GetCurrentMapRecord();

    if (!_mapPickRecordManager.IsInMap)
    {
        if(mapRecord == null)
        {
            return new MapRecordViewModel(false, MapType.Hideout, "");
        }
        else
        {
            // ✅ 如果有記錄但不在地圖中，標記為未完成
            mapRecord.IsIncomplete = true;
            return mapRecord;
        }
    }
    // ...
}
```

### 前端可根據狀態顯示不同 UI
- `IsInMap = true, IsIncomplete = false` → 正常進行中
- `IsInMap = false, IsIncomplete = true` → 未完成的地圖（可顯示警告或提示）
- `IsInMap = false, IsIncomplete = false` → 已完成或未開始

---

## 測試建議

### 單元測試場景
1. ✅ 正常開始和結束地圖
2. ✅ 遊戲關閉時標記未完成
3. ✅ 返回避難所時不影響 `IsIncomplete`
4. ✅ 重置時清空所有狀態

### 整合測試場景
1. ✅ 完整遊玩一張地圖
2. ✅ 進入地圖後遊戲關閉
3. ✅ 進入地圖後返回避難所再結束

---

## 優點

### ✅ 語意清晰
- `MarkCurrentMapAsIncomplete()` 明確表達「標記為未完成」的意圖
- 移除了語意混亂的 `SetIsInMap(bool)` 方法

### ✅ 職責單一
- `MapPickRecordManager` 是狀態的唯一來源
- `MapRecordViewModel` 只負責傳遞狀態

### ✅ 邏輯正確
- `IsIncomplete` 只在真正異常時才設為 `true`
- `StartMapRecord` 和 `EndMapRecord` 自動管理狀態，減少錯誤

### ✅ 易於維護
- 狀態變更有明確的日誌記錄
- 方法命名清楚，易於理解

---

## 相關檔案

- `src\TorchLight.Statistics\Services\MapPickRecordManager.cs`
- `src\TorchLight.Statistics\Models\MapRecordViewModel.cs`
- `src\TorchLight.Statistics\Models\MapRecordModel.cs`
- `src\TorchLight.Statistics\LogProcessor\GameLogProcessor.cs`

---

**文件版本**: 1.0  
**最後更新**: 2025/01/XX  
**維護者**: TorchLight Statistics Team
