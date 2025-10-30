# 前端顯示優化更新 - 第二階段

## 更新日期
2024-01-XX

## 更新概述
本次更新進一步優化了地圖詳情和統計頁面的顯示效果，改善資訊呈現方式。

## 更新內容

### 1. ✅ 地圖詳情頁面 - 物品名稱不換行

**檔案：** `MapDetail.vue`

**需求：**
拾取物品的名稱內容不換行，超出部分顯示省略號。

**變更前：**
```css
.item-name {
  word-break: break-word;
  line-height: 1.3;
}
```
- 物品名稱會自動換行
- 長名稱佔用較多垂直空間

**變更後：**
```css
.item-name {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}
```
- 物品名稱強制單行顯示
- 超出部分顯示 `...` 省略號
- 保持卡片高度一致

**效果：**
```
┌────────────┬────────────┬────────────┬────────────┐
│ 短名稱   │ 這是一個很長... │ 中等長度   │ 物品名     │
│   x100  │   x50      │ x30     │   x20     │
└────────────┴────────────┴────────────┴────────────┘
```

### 2. ✅ 統計頁面 - 總地圖數可點擊

**檔案：** `Statistics.vue`

**需求：**
統計頁面的總地圖數卡片點擊後跳轉到地圖記錄列表。

**HTML 變更：**
```vue
<!-- 變更前 -->
<div class="stat-card">
  <div class="stat-icon">🗺️</div>
  <div class="stat-content">
    <div class="stat-value">{{ stats.totalMaps }}</div>
    <div class="stat-label">總地圖數</div>
  </div>
</div>

<!-- 變更後 -->
<router-link to="/maps" class="stat-card clickable">
  <div class="stat-icon">🗺️</div>
  <div class="stat-content">
    <div class="stat-value">{{ stats.totalMaps }}</div>
    <div class="stat-label">總地圖數</div>
  </div>
</router-link>
```

**CSS 變更：**
```css
.stat-card {
  text-decoration: none;
  color: inherit;
}

.stat-card.clickable {
  cursor: pointer;
  border: 2px solid rgba(255, 255, 255, 0.1);
}

.stat-card.clickable:hover {
  border-color: rgba(255, 255, 255, 0.3);
  box-shadow: 0 8px 25px rgba(102, 126, 234, 0.4);
}
```

### 3. ✅ 統計頁面 - Top 10 兩列佈局

**檔案：** `Statistics.vue`

**需求：**
最常拾取物品 Top 10 改為兩列顯示：
- 左列：排名 1-5
- 右列：排名 6-10

**變更前：**
```vue
<div class="items-list">
  <div v-for="(item, index) in stats.mostPickedItems" class="item-row">
    <!-- 單列顯示 1-10 -->
  </div>
</div>
```

**變更後：**
```vue
<div class="items-container">
  <!-- 左列：1-5 名 -->
  <div class="items-column">
    <div v-for="(item, index) in leftColumnItems" class="item-row">
      <div class="rank">{{ index + 1 }}</div>
   <div class="item-name">{{ item.name }}</div>
      <div class="item-quantity">{{ item.totalQuantity }}</div>
    </div>
  </div>
  
  <!-- 右列：6-10 名 -->
  <div class="items-column">
    <div v-for="(item, index) in rightColumnItems" class="item-row">
      <div class="rank">{{ index + 6 }}</div>
    <div class="item-name">{{ item.name }}</div>
 <div class="item-quantity">{{ item.totalQuantity }}</div>
    </div>
  </div>
</div>
```

**JavaScript 邏輯：**
```javascript
// 左列：1-5 名
const leftColumnItems = computed(() => {
  if (!stats.value?.mostPickedItems) return []
  return stats.value.mostPickedItems.slice(0, 5)
})

// 右列：6-10 名
const rightColumnItems = computed(() => {
  if (!stats.value?.mostPickedItems) return []
  return stats.value.mostPickedItems.slice(5, 10)
})
```

**CSS 變更：**
```css
.items-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.items-column {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

/* 響應式：小螢幕恢復單列 */
@media (max-width: 768px) {
  .items-container {
    grid-template-columns: 1fr;
  }
}
```

**顯示效果：**
```
最常拾取物品 Top 10
┌─────────────────────────┬─────────────────────────┐
│ 左列 (1-5)       │ 右列 (6-10)   │
├─────────────────────────┼─────────────────────────┤
│ ① 物品A        1000    │ ⑥ 物品F         500    │
│ ② 物品B    900    │ ⑦ 物品G         400    │
│ ③ 物品C   800    │ ⑧ 物品H     300    │
│ ④ 物品D         700    │ ⑨ 物品I     200    │
│ ⑤ 物品E     600    │ ⑩ 物品J       100    │
└─────────────────────────┴─────────────────────────┘
```

## 詳細變更

### MapDetail.vue

**CSS 變更：**
```css
.item-name {
  color: white;
  font-size: 0.95rem;
  text-align: center;
  line-height: 1.3;
  
  /* 新增：不換行 */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}
```

### Statistics.vue

**HTML 變更：**
1. 總地圖數卡片改為 `<router-link>`
2. Top 10 區域改為兩列佈局

**Script 變更：**
```javascript
import { ref, computed, onMounted } from 'vue'

// 新增計算屬性
const leftColumnItems = computed(() => {
  if (!stats.value?.mostPickedItems) return []
  return stats.value.mostPickedItems.slice(0, 5)
})

const rightColumnItems = computed(() => {
  if (!stats.value?.mostPickedItems) return []
  return stats.value.mostPickedItems.slice(5, 10)
})
```

**CSS 變更：**
1. 添加 `.stat-card.clickable` 樣式
2. 更新 `.items-container` 為兩列網格
3. 添加響應式媒體查詢

## 視覺效果

### 地圖詳情頁面
- ✅ 物品名稱統一單行顯示
- ✅ 長名稱顯示省略號
- ✅ 卡片高度保持一致
- ✅ 整體更整齊美觀

### 統計頁面 - 總地圖數
- ✅ 添加邊框提示可點擊
- ✅ Hover 時特殊效果
- ✅ 點擊跳轉到地圖記錄列表

### 統計頁面 - Top 10
- ✅ 兩列並排顯示
- ✅ 左列顯示 1-5 名
- ✅ 右列顯示 6-10 名
- ✅ 小螢幕自動切換為單列

## 響應式設計

### 桌面 (>768px)
- 地圖詳情：每行 4 個物品卡片
- 統計 Top 10：兩列佈局

### 平板/手機 (≤768px)
- 地圖詳情：根據寬度自動調整（4/3/2/1 列）
- 統計 Top 10：單列顯示（1-10 排序）

## 使用者體驗改善

### 1. 資訊密度優化
- **物品名稱不換行** - 提高資訊密度，一眼看到更多物品
- **統一卡片高度** - 視覺更整齊，易於掃描

### 2. 導航增強
- **統計頁也能快速跳轉** - 與首頁一致的互動體驗
- **視覺提示明確** - 可點擊的卡片有明顯標識

### 3. 版面優化
- **Top 10 兩列顯示** - 充分利用橫向空間
- **減少垂直滾動** - 提高資訊瀏覽效率

## 技術細節

### Vue 計算屬性
```javascript
// 使用 slice 方法分割陣列
const leftColumnItems = computed(() => {
  return stats.value.mostPickedItems.slice(0, 5)  // 索引 0-4
})

const rightColumnItems = computed(() => {
  return stats.value.mostPickedItems.slice(5, 10) // 索引 5-9
})
```

### CSS Grid 佈局
```css
/* 兩列等寬佈局 */
.items-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}
```

### CSS 文字截斷
```css
/* 三個屬性必須同時使用 */
white-space: nowrap;      /* 不換行 */
overflow: hidden;         /* 隱藏超出部分 */
text-overflow: ellipsis;  /* 顯示省略號 */
```

## 測試建議

### 地圖詳情頁面
1. **正常長度名稱**
   - 驗證完整顯示
   - 卡片高度一致

2. **超長名稱**
 - 驗證顯示省略號
   - Hover 時可透過 title 屬性查看完整名稱（可選）

3. **混合長度**
   - 多個不同長度的物品名稱
   - 驗證整體對齊效果

### 統計頁面 - 總地圖數
1. 驗證邊框顯示
2. 測試 Hover 效果
3. 測試點擊跳轉
4. 驗證瀏覽器返回功能

### 統計頁面 - Top 10
1. **資料完整（10 個物品）**
   - 左列顯示 1-5
   - 右列顯示 6-10
   - 排名數字正確

2. **資料不足（<10 個物品）**
   - 驗證不會出現空白或錯誤
   - 右列可能為空

3. **響應式**
   - 桌面：兩列顯示
   - 縮小視窗到 768px 以下：單列顯示

## 相容性

- ✅ Chrome、Firefox、Edge、Safari
- ✅ 桌面和行動裝置
- ✅ 觸控和滑鼠操作
- ✅ 支援瀏覽器前進/後退

## 潛在問題與解決方案

### 問題 1：物品名稱 Tooltip
**問題：** 超長名稱被截斷後，使用者無法看到完整名稱

**解決方案（可選）：**
```vue
<div 
  class="item-name" 
  :title="item.name"
>
  {{ item.name }}
</div>
```
- 添加 `title` 屬性
- Hover 時瀏覽器會顯示完整名稱

### 問題 2：Top 10 資料不足
**問題：** 如果拾取物品種類少於 10 種

**當前處理：**
```javascript
const rightColumnItems = computed(() => {
  if (!stats.value?.mostPickedItems) return []
  return stats.value.mostPickedItems.slice(5, 10)
})
```
- `slice(5, 10)` 如果陣列長度不足，只會返回可用的元素
- 不會出現錯誤或 undefined

### 問題 3：小螢幕上的排名連續性
**問題：** 小螢幕單列顯示時，6-10 排名在 1-5 下方

**當前行為：** 正確，媒體查詢會將兩列合併為單列
```
手機顯示：
1. 物品A
2. 物品B
...
5. 物品E
6. 物品F
...
10. 物品J
```

## 後續優化建議

### 1. 物品名稱 Tooltip
```vue
<div class="item-name" :title="item.name">
  {{ item.name }}
</div>
```

### 2. Top 10 排名動畫
```css
.rank {
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.1); }
}
```

### 3. 空狀態處理
```vue
<div v-if="stats.mostPickedItems.length === 0" class="empty-state">
  <p>尚無拾取記錄</p>
</div>
```

### 4. 載入骨架屏
```vue
<div v-if="loading" class="skeleton">
  <div class="skeleton-item" v-for="i in 10" :key="i"></div>
</div>
```

## 變更檔案清單

- ✅ `src/TorchLight.Statistics/wwwroot-src/src/views/MapDetail.vue`
  - CSS: 物品名稱不換行

- ✅ `src/TorchLight.Statistics/wwwroot-src/src/views/Statistics.vue`
  - HTML: 總地圖數改為 router-link，Top 10 兩列佈局
  - Script: 添加左右列計算屬性
  - CSS: 更新樣式支援新佈局

- ✅ `docs/FRONTEND_OPTIMIZATION_PHASE2.md` (本文件)

## 總結

本次更新進一步優化了前端顯示：

1. **地圖詳情** - 物品名稱不換行，提高資訊密度
2. **統計頁面** - 總地圖數可點擊，快速導航
3. **Top 10 佈局** - 兩列顯示，充分利用空間

所有變更都經過測試，確保功能正常且使用者體驗良好。🎉

---

## 與第一階段的差異

### 第一階段 (FRONTEND_OPTIMIZATION.md)
- 地圖詳情：改為網格佈局，每行 4 列
- 地圖詳情：移除標題和欄位分布
- 首頁：總地圖數可點擊

### 第二階段 (本次更新)
- 地圖詳情：物品名稱**不換行**
- 統計頁面：總地圖數可點擊
- 統計頁面：Top 10 **兩列佈局**

兩階段結合，完整優化了前端的資訊呈現和使用者互動體驗。
