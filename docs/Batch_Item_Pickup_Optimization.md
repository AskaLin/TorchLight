# 批次物品拾取通知優化

## 🔍 問題分析

### 原有設計缺陷

**問題 1：逐個發送物品**
```csharp
// ❌ 問題：5 種物品會分 5 次發送
foreach (var item in mergedItems)
{
    await _webViewHub.NotifyItemPickedAsync(item.ItemName, item.TotalQuantity);
}
```

**問題 2：未納管的 bagSyncStatus 通知**
```csharp
// ❌ MainWindow.cs 直接呼叫 WebViewHub，繞過節流器
private async void HandleBagSyncCompleted()
{
    await _webViewHub.NotifyBagSyncStatusAsync(DateTime.Now);  // 未節流！
}

public async Task NotifyBagSyncAsync()
{
    await _webViewHub.NotifyBagSyncStatusAsync(DateTime.Now);  // 未節流！
}

// ❌ WebViewApi.cs 手動結算地圖時也繞過節流
_mainWindow.Invoke(async () =>
{
    await _mainWindow.NotifyBagSyncAsync();  // 未節流！
});
```

**實際情況範例**：
```
200ms 內拾取了以下物品：
- 神聖石 x3
- 神聖石 x7
- 混沌石 x5
- 鏡子碎片 x2
- 神聖石 x10
- 鏡子碎片 x3
- 混沌石 x3
- 崇高石 x1
- 崇高石 x4

合併後：
- 神聖石 x20 (3+7+10)
- 混沌石 x8 (5+3)
- 鏡子碎片 x5 (2+3)
- 崇高石 x5 (1+4)

原有做法：發送 4 次通知 ❌
- 通知 1: 神聖石 x20
- 通知 2: 混沌石 x8
- 通知 3: 鏡子碎片 x5
- 通知 4: 崇高石 x5
```

### 效率問題

| 項目 | 原有方案 | 問題 |
|------|---------|------|
| **網路通訊** | 5 次 | 每次都有通訊開銷（序列化、傳輸、反序列化） |
| **前端處理** | 5 次事件觸發 | 可能觸發 5 次 UI 重繪 |
| **日誌記錄** | 5 次 | 產生大量日誌 |
| **JavaScript 執行** | 5 次 | 需要 5 次 postMessage 和事件處理 |

---

## 💡 優化方案

### 新設計：一次批次發送

**優化後實作**：
```csharp
// ✅ 改進：5 種物品一次發送完成
await _webViewHub.NotifyItemsPickedBatchAsync(
    mergedItems.Select(item => new
    {
        item.ItemName,
        Quantity = item.TotalQuantity
    }).ToArray()
);
```

**實際情況範例**：
```
合併後的資料：
- 神聖石 x20
- 混沌石 x8
- 鏡子碎片 x5
- 崇高石 x5

優化後做法：發送 1 次批次通知 ✅
通知 1: [
  { itemName: "神聖石", quantity: 20 },
  { itemName: "混沌石", quantity: 8 },
  { itemName: "鏡子碎片", quantity: 5 },
  { itemName: "崇高石", quantity: 5 }
]
```

---

## 📊 效能對比

### 場景：200ms 內拾取 9 筆物品，合併為 5 種

| 項目 | 原有方案（逐個發送） | 優化方案（批次發送） | 改善幅度 |
|------|-------------------|-------------------|---------|
| **網路通訊次數** | 5 次 | 1 次 | **減少 80%** 🎉 |
| **前端事件觸發** | 5 次 | 1 次 | **減少 80%** 🎉 |
| **UI 重繪次數** | 可能 5 次 | 1 次 | **減少 80%** 🎉 |
| **JavaScript 執行** | 5 次 postMessage | 1 次 postMessage | **減少 80%** 🎉 |
| **總資料傳輸量** | 5 個 JSON 封包 | 1 個 JSON 陣列 | **減少約 40%**（減少封包頭） |

### 場景：大量拾取（100 筆物品，合併為 20 種）

| 項目 | 原有方案 | 優化方案 | 改善幅度 |
|------|---------|---------|---------|
| **網路通訊次數** | 20 次 | 1 次 | **減少 95%** 🎉 |
| **前端處理負擔** | 20 次 | 1 次 | **減少 95%** 🎉 |

---

## 🔧 技術實作

### 後端（C#）

#### WebViewNotificationThrottle.cs
```csharp
/// <summary>
/// 批次發送物品拾取通知（一次發送所有物品）
/// </summary>
private void FlushItemPickedNotifications(List<NotificationItem> notifications)
{
    // 1️⃣ 合併相同物品的數量
    var mergedItems = notifications
        .GroupBy(n => n.ItemName)
        .Select(g => new
        {
            ItemName = g.Key,
            TotalQuantity = g.Sum(n => n.Quantity),
            Count = g.Count()
        })
        .ToList();

    // 2️⃣ 一次性發送所有物品
    _ = Task.Run(async () =>
    {
        try
        {
            // 🔥 關鍵：改為批次發送
            await _webViewHub.NotifyItemsPickedBatchAsync(
                mergedItems.Select(item => new
                {
                    item.ItemName,
                    Quantity = item.TotalQuantity
                }).ToArray()
            );

            Log.Debug("批次發送物品拾取通知：{Count} 筆原始通知合併為 {MergedCount} 種物品，一次發送完成",
                notifications.Count, mergedItems.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批次發送物品拾取通知失敗");
        }
    });
}
```

#### WebViewHub.cs
```csharp
/// <summary>
/// 🆕 通知前端：批次物品拾取（一次發送多個物品）
/// </summary>
public Task NotifyItemsPickedBatchAsync(object[] items)
{
    return SendMessageAsync("itemsPickedBatch", new
    {
        items,
        count = items.Length
    });
}
```

### 前端（JavaScript）

#### 接收批次通知
```javascript
// 🆕 接收批次物品拾取通知
window.addEventListener('message', (event) => {
  if (event.data.type === 'itemsPickedBatch') {
    const { items, count } = event.data.data
    
    console.log(`收到批次拾取通知：${count} 種物品`)
    
    // 一次處理所有物品
    items.forEach(item => {
      console.log(`${item.itemName} x${item.quantity}`)
      // 更新 UI（只觸發一次重繪）
    })
    
    // 🎉 只更新一次 UI
    updateItemListUI(items)
  }
})
```

#### 向後相容（保留舊的單一物品通知）
```javascript
// ⚠️ 舊版通知（保留以防需要）
window.addEventListener('message', (event) => {
  if (event.data.type === 'itemPicked') {
    const { itemName, quantity } = event.data.data
    console.log(`拾取: ${itemName} x${quantity}`)
  }
})
```

---

## 📦 資料格式

### 舊格式（逐個發送）
```json
// 通知 1
{
  "type": "itemPicked",
  "data": {
    "itemName": "神聖石",
    "quantity": 20
  }
}

// 通知 2
{
  "type": "itemPicked",
  "data": {
    "itemName": "混沌石",
    "quantity": 8
  }
}

// ... 3 次更多通知
```

### 新格式（批次發送）
```json
// 只有 1 個通知
{
  "type": "itemsPickedBatch",
  "data": {
    "items": [
      { "itemName": "神聖石", "quantity": 20 },
      { "itemName": "混沌石", "quantity": 8 },
      { "itemName": "鏡子碎片", "quantity": 5 },
      { "itemName": "崇高石", "quantity": 5 }
    ],
    "count": 4
  }
}
```

**資料大小對比**（未壓縮）：
- 舊格式：約 400 bytes（5 個獨立 JSON）
- 新格式：約 240 bytes（1 個 JSON 陣列）
- **節省：40%** 🎉

---

## 🎯 優點總結

### 1️⃣ 網路效能
- **減少通訊次數**：5 次 → 1 次
- **減少封包開銷**：每個 HTTP/WebSocket 封包都有頭部開銷
- **降低延遲**：一次發送完成，減少往返時間

### 2️⃣ 前端效能
- **減少事件觸發**：只觸發一次 `message` 事件
- **減少 UI 重繪**：可以批次更新 DOM，只重繪一次
- **降低 JavaScript 執行次數**：處理迴圈在前端執行，更高效

### 3️⃣ 程式碼品質
- **更清晰的語意**：「批次拾取」比「逐個拾取」更符合實際情況
- **更容易維護**：前端只需處理一種批次格式
- **更好的擴展性**：未來可以加入更多批次資訊（如總價值、稀有度等）

### 4️⃣ 用戶體驗
- **更流暢的 UI**：避免多次閃爍
- **更即時的反饋**：所有拾取資訊一次顯示
- **更好的視覺效果**：可以做批次動畫效果

---

## 🔄 遷移指南

### 前端需要的變更

#### 舊程式碼（逐個處理）
```javascript
// ❌ 舊版：每次只處理一個物品
window.addEventListener('message', (event) => {
  if (event.data.type === 'itemPicked') {
    addItemToList(event.data.data.itemName, event.data.data.quantity)
  }
})
```

#### 新程式碼（批次處理）- ✅ 已實作

**src/stores/mapStore.js**
```javascript
// ✅ 新版：一次處理所有物品
const handleBackendMessage = (message) => {
  switch (message.type) {
    // 🆕 批次物品拾取通知（優先處理）
    case 'itemsPickedBatch':
      if (message.data && message.data.items) {
        const items = message.data.items
        const count = message.data.count
        console.log(`📦 批次拾取通知：${count} 種物品`, items)
        
        // 批次顯示拾取的物品
        items.forEach(item => {
          console.log(`  - ${item.itemName} x${item.quantity}`)
        })
        
        // 🎯 只重新載入一次當前地圖資訊
        refreshCurrentMap()
      }
      break

    // 🔄 保留舊版單一物品拾取（向後相容）
    case 'itemPicked':
      console.log('Item picked:', message.data)
      refreshCurrentMap()
      break
  }
}
```

**進階範例：批次更新 UI（如果需要）**
```javascript
// 使用 DocumentFragment 批次插入 DOM
function batchAddItemsToList(items) {
  // 使用 DocumentFragment 批次插入 DOM
  const fragment = document.createDocumentFragment()
  
  items.forEach(item => {
    const element = createItemElement(item.itemName, item.quantity)
    fragment.appendChild(element)
  })
  
  // 🎉 只觸發一次 DOM 重繪
  document.getElementById('item-list').appendChild(fragment)
}
```

**通知顯示範例（可選）**
```javascript
case 'itemsPickedBatch':
  if (message.data && message.data.items) {
    const items = message.data.items
    
    // 🆕 顯示批次拾取通知（可選）
    showNotification(
      'success',
      `拾取 ${items.length} 種物品`,
      items.map(i => `${i.itemName} x${i.quantity}`).join('\n')
    )
    
    refreshCurrentMap()
  }
  break
