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
    <div v-else-if="mapStore.currentMapInfo.mapType === 'Netherrealm'" class="netherrealm-info">
    <!-- 地圖基本資訊 -->
      <div class="map-header">
        <div class="map-name-large">{{ mapStore.currentMapInfo.mapName }}</div>
     <div class="map-time" v-if="mapStore.currentMapInfo.startTime">
          <span class="time-label">進圖時間</span>
       <span class="time-value">{{ formatStartTime }}</span>
     </div>
      </div>

      <!-- 開圖材料 -->
      <div class="map-materials">
        <!-- 門票 -->
      <div class="material-item" v-if="mapStore.currentMapInfo.mapTicket">
   <span class="material-icon">🎟️</span>
     <div class="material-content">
            <span class="material-label">門票</span>
  <span class="material-value">{{ mapStore.currentMapInfo.mapTicket }}</span>
          </div>
   </div>

     <!-- 羅盤 -->
        <div class="material-item" v-if="compassList.length > 0">
  <span class="material-icon">🧭</span>
  <div class="material-content">
    <span class="material-label">羅盤</span>
            <div class="compass-list">
  <span v-for="(compass, index) in compassList" :key="index" class="compass-item">
     {{ compass }}
   </span>
            </div>
          </div>
        </div>

        <!-- 探針 -->
        <div class="material-item" v-if="mapStore.currentMapInfo.probe">
      <span class="material-icon">📍</span>
          <div class="material-content">
          <span class="material-label">探針</span>
      <span class="material-value">{{ mapStore.currentMapInfo.probe }}</span>
    </div>
        </div>
      </div>

      <!-- 拾取物品資訊 -->
      <div class="picked-items" v-if="items.length > 0">
        <h4 class="items-title">拾取物品</h4>
        <div class="items-grid">
   <div v-for="item in items" :key="item.baseId" class="item-card">
    <div class="item-name">{{ item.name }}</div>
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
</template>

<script setup>
import { computed } from 'vue'
import { useMapStore } from '../stores/mapStore'

const mapStore = useMapStore()

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
    second: '2-digit'
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

.map-name-large {
  color: white;
  font-size: 1.8rem;
  font-weight: bold;
  margin-bottom: 10px;
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

/* 開圖材料 */
.map-materials {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.material-item {
  display: flex;
  align-items: flex-start;
  gap: 15px;
  padding: 15px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.material-icon {
  font-size: 1.8rem;
}

.material-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.material-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.85rem;
}

.material-value {
  color: white;
  font-size: 1.1rem;
  font-weight: 500;
}

.compass-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.compass-item {
  padding: 6px 12px;
  background: rgba(33, 150, 243, 0.3);
  border: 1px solid rgba(33, 150, 243, 0.5);
  border-radius: 6px;
  color: white;
  font-size: 0.95rem;
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

.item-name {
  color: white;
  font-size: 0.95rem;
  text-align: center;
  word-break: break-word;
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
}

@media (max-width: 600px) {
  .items-grid {
 grid-template-columns: 1fr;
  }
}
</style>
