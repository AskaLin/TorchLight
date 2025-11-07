# 前端批次物品拾取實作說明

## ✅ 已完成的前端變更

### 檔案：`src/stores/mapStore.js`

#### 新增批次拾取事件處理

```javascript
// 🆕 批次物品拾取通知（優先處理）
case 'itemsPickedBatch':
  if (message.data && message.data.items) {
    const items = message.data.items
    const count = message.data.count
    console.log(`📦 批次拾取通知：${count} 種物品`, items)
    
    // 🎯 只重新載入一次當前地圖資訊
    refreshCurrentMap()
  }
  break
```

#### 保留舊版相容性

```javascript
// 🔄 保留舊版單一物品拾取（向後相容）
case 'itemPicked':
  console.log('Item picked:', message.data)
  refreshCurrentMap()
  break
```

---

## 📊 資料格式

### 後端發送的批次通知

```json
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
  },
  "timestamp": "2025-01-15T10:30:45.123Z"
}
```

### 前端接收處理

```javascript
const items = message.data.items  // 陣列：[{itemName, quantity}, ...]
const count = message.data.count  // 數量：4

// 處理每個物品
items.forEach(item => {
  console.log(`${item.itemName} x${item.quantity}`)
})
```

---

## 🎯 核心邏輯

### 1. 事件優先級

```
itemsPickedBatch（優先）
    ↓
如果沒有 itemsPickedBatch，回退到 itemPicked
    ↓
refreshCurrentMap() 只呼叫一次
```

### 2. 減少 API 呼叫

**原有流程（5 個物品）：**
```
物品 1 拾取 → itemPicked → refreshCurrentMap()
物品 2 拾取 → itemPicked → refreshCurrentMap()
物品 3 拾取 → itemPicked → refreshCurrentMap()
物品 4 拾取 → itemPicked → refreshCurrentMap()
物品 5 拾取 → itemPicked → refreshCurrentMap()

= 5 次 refreshCurrentMap() 呼叫
```

**優化後流程（5 個物品）：**
```
物品 1-5 批次拾取 → itemsPickedBatch → refreshCurrentMap()

= 1 次 refreshCurrentMap() 呼叫
```

**減少：80%**（5 次 → 1 次）

---

## 🚀 效能提升

### 場景：快速拾取 10 個物品（合併為 5 種）

| 項目 | 原有方案 | 優化方案 | 改善 |
|------|---------|---------|------|
| **後端通知次數** | 10 次 | 1 次 | **減少 90%** |
| **前端事件觸發** | 10 次 | 1 次 | **減少 90%** |
| **API 呼叫次數** | 10 次 | 1 次 | **減少 90%** |
| **UI 重繪次數** | 可能 10 次 | 1 次 | **大幅提升流暢度** |

---

## 📝 前端可選的進階功能

### 1. 批次通知顯示（可選）

```javascript
case 'itemsPickedBatch':
  if (message.data && message.data.items) {
    const items = message.data.items
    
    // 🆕 顯示批次拾取通知
    const itemList = items
      .map(i => `${i.itemName} x${i.quantity}`)
      .join('\n')
    
    showNotification(
      'success',
      `拾取 ${items.length} 種物品`,
      itemList,
      3000  // 3 秒後自動關閉
    )
    
    refreshCurrentMap()
  }
  break
```

**效果：**
```
┌────────────────────────────┐
│ ✅ 拾取 4 種物品          │
├────────────────────────────┤
│ 神聖石 x20                 │
│ 混沌石 x8                  │
│ 鏡子碎片 x5                │
│ 崇高石 x5                  │
└────────────────────────────┘
```

### 2. 批次動畫效果（可選）

```javascript
case 'itemsPickedBatch':
  if (message.data && message.data.items) {
    const items = message.data.items
    
    // 🆕 批次動畫效果
    items.forEach((item, index) => {
      setTimeout(() => {
        showItemPickupAnimation(item.itemName, item.quantity)
      }, index * 100)  // 每個物品間隔 100ms
    })
    
    refreshCurrentMap()
  }
  break
```

**效果：** 物品依序淡入，但總時間只需 400ms（比逐個通知快得多）

### 3. 合併顯示相同物品（可選）

```javascript
case 'itemsPickedBatch':
  if (message.data && message.data.items) {
    const items = message.data.items
    
    // 🆕 將物品按類型分組顯示
    const categorized = categorizeItems(items)
    
    Object.entries(categorized).forEach(([category, itemList]) => {
      showCategoryNotification(category, itemList)
    })
    
    refreshCurrentMap()
  }
  break

function categorizeItems(items) {
  // 假設有 itemType 資訊
  return items.reduce((acc, item) => {
    const type = item.itemType || 'Other'
    if (!acc[type]) acc[type] = []
    acc[type].push(item)
    return acc
  }, {})
}
```

---

## 🧪 測試建議

### 1. 功能測試

```javascript
// 測試批次拾取 5 種物品
const testBatchPickup = () => {
  const mockMessage = {
    type: 'itemsPickedBatch',
    data: {
      items: [
        { itemName: '神聖石', quantity: 20 },
        { itemName: '混沌石', quantity: 8 },
        { itemName: '鏡子碎片', quantity: 5 },
        { itemName: '崇高石', quantity: 5 },
        { itemName: '命運卡片', quantity: 3 }
      ],
      count: 5
    }
  }
  
  handleBackendMessage(mockMessage)
  
  // 驗證：
  // ✅ 只呼叫一次 refreshCurrentMap()
  // ✅ console.log 顯示 5 種物品
}
```

### 2. 向後相容測試

```javascript
// 測試舊版單一物品拾取仍然正常
const testLegacyPickup = () => {
  const mockMessage = {
    type: 'itemPicked',
    data: {
      itemName: '神聖石',
      quantity: 1
    }
  }
  
  handleBackendMessage(mockMessage)
  
  // 驗證：
  // ✅ refreshCurrentMap() 仍然被呼叫
  // ✅ console.log 顯示物品資訊
}
```

### 3. 壓力測試

```javascript
// 測試大量物品批次拾取
const testLargeBatch = () => {
  const items = Array.from({ length: 50 }, (_, i) => ({
    itemName: `物品${i + 1}`,
    quantity: Math.floor(Math.random() * 100) + 1
  }))
  
  const mockMessage = {
    type: 'itemsPickedBatch',
    data: { items, count: items.length }
  }
  
  handleBackendMessage(mockMessage)
  
  // 驗證：
  // ✅ 效能沒有明顯下降
  // ✅ UI 沒有卡頓
}
```

---

## 🎨 UI/UX 改進建議

### 1. 批次拾取指示器

```vue
<template>
  <div v-if="recentBatchPickup" class="batch-pickup-indicator">
    <div class="indicator-icon">📦</div>
    <div class="indicator-text">
      批次拾取 {{ recentBatchPickup.count }} 種物品
    </div>
    <div class="indicator-items">
      <span v-for="item in recentBatchPickup.items" :key="item.itemName">
        {{ item.itemName }} x{{ item.quantity }}
      </span>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const recentBatchPickup = ref(null)

// 在 handleBackendMessage 中設定
case 'itemsPickedBatch':
  recentBatchPickup.value = message.data
  
  // 3 秒後自動隱藏
  setTimeout(() => {
    recentBatchPickup.value = null
  }, 3000)
  break
</script>
```

### 2. 拾取歷史記錄

```javascript
// 在 mapStore.js 中添加
const pickupHistory = ref([])

case 'itemsPickedBatch':
  if (message.data && message.data.items) {
    // 🆕 記錄到歷史
    pickupHistory.value.unshift({
      timestamp: Date.now(),
      items: message.data.items,
      count: message.data.count
    })
    
    // 🆕 只保留最近 20 筆
    if (pickupHistory.value.length > 20) {
      pickupHistory.value.pop()
    }
    
    refreshCurrentMap()
  }
  break
```

### 3. 拾取統計圖表

```javascript
// 計算拾取頻率
const pickupFrequency = computed(() => {
  return pickupHistory.value.reduce((acc, batch) => {
    batch.items.forEach(item => {
      acc[item.itemName] = (acc[item.itemName] || 0) + item.quantity
    })
    return acc
  }, {})
})

// 顯示最常拾取的物品
const topPickedItems = computed(() => {
  return Object.entries(pickupFrequency.value)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 10)
})
```

---

## 📚 開發模式模擬資料

### 更新 `src/utils/api.js`（可選）

```javascript
// 🆕 模擬批次拾取通知
function simulateBatchPickup() {
  const mockMessage = {
    type: 'itemsPickedBatch',
    data: {
      items: [
        { itemName: '神聖石', quantity: 20 },
        { itemName: '混沌石', quantity: 8 },
        { itemName: '鏡子碎片', quantity: 5 }
      ],
      count: 3
    },
    timestamp: new Date().toISOString()
  }
  
  window.postMessage(mockMessage, '*')
}

// 開發模式下定時模擬
if (import.meta.env.DEV) {
  setInterval(simulateBatchPickup, 10000)  // 每 10 秒模擬一次
}
```

---

## 🎉 總結

### ✅ 前端已完成

1. **批次拾取事件處理** - 新增 `itemsPickedBatch` 處理邏輯
2. **向後相容** - 保留 `itemPicked` 事件處理
3. **效能優化** - 減少 80-90% API 呼叫次數
4. **日誌記錄** - 清楚顯示批次拾取資訊

### 🎯 核心優化

- **減少 API 呼叫**：5 個物品從 5 次 → 1 次
- **減少 UI 重繪**：可能從 5 次 → 1 次
- **更好的用戶體驗**：UI 更流暢，反饋更清晰

### 🚀 可選功能（未來擴展）

- 批次拾取通知顯示
- 批次動畫效果
- 拾取歷史記錄
- 拾取統計圖表

---

**文件版本**: 1.0  
**最後更新**: 2025/01/XX  
**維護者**: TorchLight Statistics Team
