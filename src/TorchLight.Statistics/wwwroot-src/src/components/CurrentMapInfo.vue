<template>
  <div class="current-map-info" v-if="mapStore.currentMapInfo.isInMap || mapStore.currentMapInfo.mapName">
    <h3 class="section-title">
      <span class="title-icon">🗺️</span>
      當前地圖資訊
    </h3>

    <!-- 避難所地圖 - 只顯示地圖名稱 -->
    <div v-if="mapStore.currentMapInfo.mapType === 'Hideout'" class="hideout-info">
      <div class="map-name-card">
        <span class="map-label">地圖名稱</span>
        <span class="map-value">{{ mapStore.currentMapInfo.mapName || '避難所' }}</span>
      </div>
    </div>

    <!-- 異界地圖 - 顯示完整資訊 -->
    <div v-else class="netherrealm-info">
      <!-- 地圖基本資訊 -->
      <div class="map-header">
        <div class="map-name-row">
          <div class="map-name-large">{{ mapStore.currentMapInfo.mapName }}</div>
          <!-- 🆕 未知地圖編輯按鈕 -->
          <button v-if="isUnknownMap" 
                  @click="openMapEdit" 
                  class="btn-edit"
                  title="編輯地圖資訊">
            ✏️ 編輯
          </button>
        </div>
        <div class="map-time" v-if="mapStore.currentMapInfo.startTime">
          <span class="time-label">進圖時間</span>
          <span class="time-value">{{ formatStartTime }}</span>
        </div>
      </div>

      <!-- 開圖材料 -->
      <div class="map-materials">
        <!-- 門票 -->
        <div class="material-item" v-if="mapStore.currentMapInfo.mapTicket">
          <div class="material-content">
            <div class="material-header">
              <span class="material-icon">🎟️</span>
              <span class="material-label">門票</span>
            </div>
            <span class="material-value">{{ mapStore.currentMapInfo.mapTicket }}</span>
          </div>
        </div>

        <!-- 迴響 -->
        <div class="material-item" v-if="mapStore.currentMapInfo.resonance > 0">
          <div class="material-content">
            <div class="material-header">
              <span class="material-icon">🎟️</span>
              <span class="material-label">異界迴響</span>
            </div>
            <span class="material-value">{{ resonance }}</span>
          </div>
        </div>

        <!-- 羅盤 -->
        <div class="material-item" v-if="compassList.length > 0">
          <div class="material-content">
            <div class="material-header">
              <span class="material-icon">🧭</span>
              <span class="material-label">羅盤</span>
            </div>
            <div class="compass-list">
              <span v-for="(compass, index) in compassList" :key="index" class="compass-item">
                {{ removeLastTwoChars(compass) }}
              </span>
            </div>
          </div>
        </div>

        <!-- 探針 -->
        <div class="material-item" v-if="mapStore.currentMapInfo.probe">
          <div class="material-content">
            <div class="material-header">
              <span class="material-icon">📍</span>
              <span class="material-label">探針</span>
            </div>
            <span class="material-value">{{ mapStore.currentMapInfo.probe }}</span>
          </div>
        </div>
      </div>

      <!-- 拾取物品資訊 -->
      <div class="picked-items" v-if="items.length > 0">
        <h4 class="items-title">拾取物品</h4>
        <div class="items-grid">
          <div v-for="item in items" :key="item.baseId" class="item-card">
            <div class="item-name-row">
              <div class="item-name">{{ item.name }}</div>
              <!-- 🆕 未知物品編輯按鈕 - 使用 ItemType 判斷 -->
              <button v-if="isUnknownItem(item)" 
                      @click="openItemEdit(item)" 
                      class="btn-edit-small"
                      title="編輯物品資訊">
                ✏️
              </button>
            </div>
            <div class="item-quantity">x{{ item.total }}</div>
          </div>
        </div>
      </div>

      <!-- 無拾取物品提示 -->
      <div v-else class="no-items">
        <span class="no-items-icon">📦</span>
        <span>尚未拾取任何物品</span>
      </div>
    </div>
  </div>

  <!-- 未在地圖中 -->
  <div v-else class="no-map-info">
    <span class="no-map-icon">🌍</span>
    <span>目前不在任何地圖中</span>
  </div>

  <!-- 🆕 編輯對話框 -->
  <EditDialog 
    :show="showEditDialog"
    :editType="editType"
    :editData="editData"
    @close="closeEditDialog"
    @saved="handleEditSaved"
  />
</template>

<script setup>
  import { ref, computed } from 'vue'
  import { useMapStore } from '../stores/mapStore'
  import EditDialog from './EditDialog.vue'

  const mapStore = useMapStore()
  
  // 編輯對話框狀態
  const showEditDialog = ref(false)
  const editType = ref('') // 'map' or 'item'
  const editData = ref({})

  // 格式化進圖時間
  const formatStartTime = computed(() => {
    if (!mapStore.currentMapInfo.startTime) return ''

    const time = new Date(mapStore.currentMapInfo.startTime)
    return time.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  })

  // 羅盤列表
  const compassList = computed(() => {
    if (!mapStore.currentMapInfo.compass) return []
    return mapStore.currentMapInfo.compass.filter(c => c && c.trim() !== '')
  })

  // 拾取物品列表
  const items = computed(() => {
    return mapStore.currentMapInfo.items || []
  })

  // 🆕 檢查是否為未知地圖
  const isUnknownMap = computed(() => {
    const mapType = mapStore.currentMapInfo.mapType
    return mapType === 'Unknown'
  })

  const resonance = computed(() => {
    return `${mapStore.currentMapInfo.resonance} 個迴響，額外 ${Math.log2(mapStore.currentMapInfo.resonance + 1)} 條詞綴 `
  })

  // 🆕 檢查是否為未知物品（使用 ItemType）
  const isUnknownItem = (item) => {
    if (!item || !item.itemType) return false
    
    // 檢查 ItemType 是否為 Unknown 開頭（Unknown, Unknown100, Unknown101, Unknown102, Unknown103）
    return item.itemType.startsWith('Unknown')
  }

  // 🆕 開啟地圖編輯
  const openMapEdit = () => {
    const mapId = mapStore.currentMapInfo.mapId || ''
    
    editType.value = 'map'
    editData.value = {
      mapName: '',
      mapId: mapId,
      mapType: 'Netherrealm'
    }
    showEditDialog.value = true
  }

  // 🆕 開啟物品編輯
  const openItemEdit = (item) => {
    console.log(item)
    editType.value = 'item'
    editData.value = {
      itemName: '',
      itemId: item.baseId,
      pageId: item.pageId || 102, // 🆕 使用實際的 PageId
      itemType: item.itemType || 'Currency',
      enabled: true,
      watch: false
    }
    showEditDialog.value = true
  }

  // 🆕 關閉編輯對話框
  const closeEditDialog = () => {
    showEditDialog.value = false
    editType.value = ''
    editData.value = {}
  }

  // 🆕 編輯儲存完成
  const handleEditSaved = () => {
    // 刷新當前地圖資訊（後端會自動更新並推送）
    console.log('編輯已儲存，等待後端更新...')
  }

  // 移除最後兩個字
  const removeLastTwoChars = (str) => {
    if (!str || str.length <= 2) return str
    return str.slice(0, -2)
  }
</script>

<style scoped>
  .current-map-info {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border-radius: 12px;
    padding: 25px;
    margin-bottom: 20px;
  }

  .section-title {
    display: flex;
    align-items: center;
    gap: 10px;
    margin: 0 0 20px 0;
    color: white;
    font-size: 1.5rem;
  }

  .title-icon {
    font-size: 1.8rem;
  }

  /* 避難所地圖樣式 */
  .hideout-info {
    padding: 10px 0;
  }

  .map-name-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 20px;
    background: rgba(255, 255, 255, 0.08);
    border-radius: 8px;
    border: 1px solid rgba(255, 255, 255, 0.1);
  }

  .map-label {
    color: rgba(255, 255, 255, 0.6);
    font-size: 1rem;
  }

  .map-value {
    color: white;
    font-size: 1.3rem;
    font-weight: 600;
  }

  /* 異界地圖樣式 */
  .netherrealm-info {
    display: flex;
    flex-direction: column;
    gap: 20px;
  }

  .map-header {
    padding: 20px;
    background: linear-gradient(135deg, rgba(156, 39, 176, 0.2), rgba(103, 58, 183, 0.2));
    border-radius: 8px;
    border: 1px solid rgba(156, 39, 176, 0.3);
  }

  /* 🆕 地圖名稱行（包含編輯按鈕） */
  .map-name-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-bottom: 10px;
  }

  .map-name-large {
    color: white;
    font-size: 1.8rem;
    font-weight: bold;
    flex: 1;
  }

  /* 🆕 編輯按鈕 */
  .btn-edit {
    padding: 8px 16px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    font-size: 0.9rem;
    font-weight: 500;
    transition: all 0.3s;
    white-space: nowrap;
  }

  .btn-edit:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
  }

  .map-time {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .time-label {
    color: rgba(255, 255, 255, 0.6);
    font-size: 0.9rem;
  }

  .time-value {
    color: #ffeb3b;
    font-size: 1rem;
    font-weight: 500;
  }

  /* 開圖材料 - 改為網格佈局並列顯示 */
  .map-materials {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
  }

  .material-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    padding: 15px;
    background: rgba(255, 255, 255, 0.05);
    border-radius: 8px;
    border: 1px solid rgba(255, 255, 255, 0.1);
    transition: all 0.3s;
  }

    .material-item:hover {
      background: rgba(255, 255, 255, 0.08);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    }

  .material-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    width: 100%;
  }

  /* 圖示和標籤的容器 - 橫向排列 */
  .material-header {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .material-icon {
    font-size: 1.5rem;
  }

  .material-label {
    color: rgba(255, 255, 255, 0.6);
    font-size: 0.85rem;
    text-align: center;
  }

  .material-value {
    color: white;
    font-size: 1rem;
    font-weight: 500;
    text-align: center;
    word-break: break-word;
    width: 100%;
  }

  /* 羅盤列表 - 改為橫向並列，最多 4 個 */
  .compass-list {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 6px;
    width: 100%;
    max-width: 400px;
  }

  .compass-item {
    padding: 6px 12px;
    background: rgba(33, 150, 243, 0.3);
    border: 1px solid rgba(33, 150, 243, 0.5);
    border-radius: 6px;
    color: white;
    font-size: 0.9rem;
    text-align: center;
    word-break: break-word;
    flex: 0 0 auto;
    min-width: 80px;
  }

  /* 拾取物品 */
  .picked-items {
    margin-top: 10px;
  }

  .items-title {
    color: white;
    font-size: 1.2rem;
    margin: 0 0 15px 0;
  }

  .items-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
  }

  .item-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: 15px 10px;
    background: rgba(76, 175, 80, 0.15);
    border: 1px solid rgba(76, 175, 80, 0.3);
    border-radius: 8px;
    transition: transform 0.2s, box-shadow 0.2s;
  }

    .item-card:hover {
      transform: translateY(-3px);
      box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
    }

  /* 🆕 物品名稱行（包含編輯按鈕） */
  .item-name-row {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    width: 100%;
  }

  .item-name {
    color: white;
    font-size: 0.95rem;
    text-align: center;
    word-break: break-word;
    flex: 1;
  }

  /* 🆕 小型編輯按鈕 */
  .btn-edit-small {
    padding: 4px 6px;
    background: rgba(102, 126, 234, 0.3);
    color: white;
    border: 1px solid rgba(102, 126, 234, 0.5);
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.7rem;
    transition: all 0.3s;
    flex-shrink: 0;
  }

  .btn-edit-small:hover {
    background: rgba(102, 126, 234, 0.5);
    transform: scale(1.1);
  }

  .item-quantity {
    color: #4caf50;
    font-size: 1.1rem;
    font-weight: bold;
  }

  .no-items {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    padding: 30px;
    color: rgba(255, 255, 255, 0.4);
    font-size: 1rem;
  }

  .no-items-icon {
    font-size: 2rem;
  }

  /* 未在地圖中 */
  .no-map-info {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 15px;
    padding: 40px;
    background: rgba(255, 255, 255, 0.03);
    border-radius: 12px;
    border: 2px dashed rgba(255, 255, 255, 0.1);
    color: rgba(255, 255, 255, 0.4);
    font-size: 1.1rem;
  }

  .no-map-icon {
    font-size: 2.5rem;
  }

  /* 響應式設計 */
  @media (max-width: 1200px) {
    .items-grid {
      grid-template-columns: repeat(3, 1fr);
    }
  }

  @media (max-width: 900px) {
    .items-grid {
      grid-template-columns: repeat(2, 1fr);
    }

    /* 材料區塊在中等螢幕改為兩列 */
    .map-materials {
      grid-template-columns: repeat(2, 1fr);
    }
  }

  @media (max-width: 600px) {
    .items-grid {
      grid-template-columns: 1fr;
    }

    /* 材料區塊在小螢幕改為單列 */
    .map-materials {
      grid-template-columns: 1fr;
    }

    /* 羅盤在小螢幕可能需要換行 */
    .compass-list {
      max-width: 100%;
    }
  }
</style>
