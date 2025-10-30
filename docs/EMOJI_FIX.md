# Emoji 圖標編碼問題修正

## 問題描述
前端 Vue 文件中的 emoji 圖標因編碼問題顯示為 `??` 或 `???`。

## 修正的文件

### 1. Home.vue
**修正位置：快速操作按鈕**

修正前：
```vue
<span class="btn-icon">??</span>  <!-- 查看地圖記錄 -->
<span class="btn-icon">??</span>  <!-- 查看統計資料 -->
<span class="btn-icon">??</span>  <!-- 匯出記錄 -->
<span class="btn-icon">???</span> <!-- 清除所有記錄 -->
```

修正後：
```vue
<span class="btn-icon">📋</span>  <!-- 查看地圖記錄 -->
<span class="btn-icon">📊</span>  <!-- 查看統計資料 -->
<span class="btn-icon">💾</span>  <!-- 匯出記錄 -->
<span class="btn-icon">🗑️</span> <!-- 清除所有記錄 -->
```

### 2. MapList.vue
**修正位置：重新載入按鈕 + 記錄卡片資訊**

修正前：
```vue
<span v-if="!mapStore.loading">?? 重新載入</span>
<span class="label">?? 門票:</span>
<span class="label">?? 羅盤:</span>
<span class="label">?? 探針:</span>
```

修正後：
```vue
<span v-if="!mapStore.loading">🔄 重新載入</span>
<span class="label">🎟️ 門票:</span>
<span class="label">🧭 羅盤:</span>
<span class="label">📍 探針:</span>
```

### 3. MapDetail.vue
**修正位置：使用材料區域**

修正前：
```vue
<span class="material-icon">??</span> <!-- 門票 -->
<span class="material-icon">??</span> <!-- 羅盤 -->
<span class="material-icon">??</span> <!-- 探針 -->
```

修正後：
```vue
<span class="material-icon">🎟️</span> <!-- 門票 -->
<span class="material-icon">🧭</span> <!-- 羅盤 -->
<span class="material-icon">📍</span> <!-- 探針 -->
```

### 4. Statistics.vue
**修正位置：統計概覽卡片**

修正前：
```vue
<div class="stat-icon">???</div> <!-- 總地圖數 -->
<div class="stat-icon">??</div>  <!-- 總物品種類 -->
<div class="stat-icon">??</div>  <!-- 總拾取數量 -->
<div class="stat-icon">??</div>  <!-- 總遊戲時間 -->
```

修正後：
```vue
<div class="stat-icon">🗺️</div> <!-- 總地圖數 -->
<div class="stat-icon">📦</div> <!-- 總物品種類 -->
<div class="stat-icon">💎</div> <!-- 總拾取數量 -->
<div class="stat-icon">⏱️</div> <!-- 總遊戲時間 -->
```

## Emoji 對照表

| 功能 | Emoji | Unicode | 說明 |
|-----|-------|---------|------|
| 查看地圖記錄 | 📋 | U+1F4CB | 剪貼板 |
| 查看統計資料 | 📊 | U+1F4CA | 條形圖 |
| 匯出記錄 | 💾 | U+1F4BE | 軟碟片 |
| 清除記錄 | 🗑️ | U+1F5D1 | 垃圾桶 |
| 重新載入 | 🔄 | U+1F504 | 重新整理 |
| 門票 | 🎟️ | U+1F39F | 門票 |
| 羅盤 | 🧭 | U+1F9ED | 指南針 |
| 探針 | 📍 | U+1F4CD | 圖釘 |
| 地圖 | 🗺️ | U+1F5FA | 世界地圖 |
| 包裹 | 📦 | U+1F4E6 | 包裹 |
| 寶石 | 💎 | U+1F48E | 鑽石 |
| 時間 | ⏱️ | U+23F1 | 碼表 |

## 根本原因

這個問題通常是因為：
1. **檔案編碼問題**：原始檔案可能不是 UTF-8 編碼
2. **複製貼上問題**：從不同來源複製 emoji 時編碼轉換錯誤
3. **編輯器設定**：編輯器的預設編碼設定不正確

## 預防措施

### 1. 確保檔案編碼
所有 Vue 文件應使用 **UTF-8 with BOM** 或 **UTF-8** 編碼：

在 Visual Studio Code 中：
- 點擊右下角的編碼（例如 "UTF-8"）
- 選擇 "Save with Encoding"
- 選擇 "UTF-8 with BOM" 或 "UTF-8"

### 2. 專案設定
在 `.editorconfig` 中添加：
```ini
[*.vue]
charset = utf-8
```

### 3. Git 設定
確保 `.gitattributes` 包含：
```
*.vue text eol=lf
*.js text eol=lf
```

### 4. 使用 Unicode 轉義（備選方案）
如果 emoji 持續有問題，可以使用 HTML 實體或 Unicode 轉義：

```vue
<!-- 方案 1: HTML 實體 -->
<span>&#x1F4CB;</span> <!-- 📋 -->

<!-- 方案 2: Unicode 轉義 -->
<span v-html="'\uD83D\uDCCB'"></span> <!-- 📋 -->

<!-- 方案 3: 直接使用 emoji（推薦） -->
<span>📋</span>
```

## 測試建議

1. **瀏覽器測試**
   - Chrome
   - Firefox
   - Edge
   - Safari

2. **作業系統測試**
   - Windows 10/11
   - macOS
   - Linux

3. **確認顯示**
   - 檢查所有頁面的 emoji 是否正確顯示
   - 確認沒有 `??` 或亂碼

## 驗證清單

- [x] Home.vue - 快速操作按鈕
- [x] MapList.vue - 重新載入按鈕和記錄資訊
- [x] MapDetail.vue - 使用材料圖標
- [x] Statistics.vue - 統計概覽圖標
- [x] CurrentMapInfo.vue - 已正確（無編碼問題）
- [x] 專案構建成功

## 注意事項

1. **Hot Reload**: 由於應用程式正在偵錯中，可能需要手動重新載入瀏覽器以查看更改
2. **快取清除**: 如果更改未生效，清除瀏覽器快取後重試
3. **前端重建**: 如果使用 Vite 或其他打包工具，可能需要重新構建前端資源

## 後續建議

1. **建立 Emoji 組件**
   創建一個專用的 emoji 組件來統一管理：

```vue
<!-- components/Emoji.vue -->
<template>
  <span class="emoji" :aria-label="label">{{ icon }}</span>
</template>

<script setup>
defineProps({
  icon: String,
  label: String
})
</script>
```

2. **建立圖標常數**
   創建一個常數文件來管理所有圖標：

```javascript
// utils/icons.js
export const ICONS = {
  MAP: '🗺️',
  ITEM: '📦',
  TICKET: '🎟️',
  COMPASS: '🧭',
  PROBE: '📍',
  // ... 更多圖標
}
```

使用方式：
```vue
<template>
  <span>{{ ICONS.MAP }}</span>
</template>

<script setup>
import { ICONS } from '@/utils/icons'
</script>
```
