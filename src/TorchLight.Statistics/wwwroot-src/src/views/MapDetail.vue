<template>
  <div class="map-detail">
  <div class="detail-header">
     <button @click="goBack" class="btn-back">← 返回列表</button>
    </div>

 <div v-if="loading" class="loading">載入中...</div>

    <div v-else-if="detail" class="detail-content">
      <div class="detail-card">
<h2>{{ detail.name }}</h2>

  <div class="detail-section">
 <h3>基本資訊</h3>
   <div class="info-grid">
   <div class="info-item">
<span class="label">地圖ID:</span>
       <span class="value">{{ detail.id }}</span>
            </div>
            <div class="info-item">
              <span class="label">開始時間:</span>
  <span class="value">{{ formatDateTime(detail.startTime) }}</span>
            </div>
            <div class="info-item">
     <span class="label">結束時間:</span>
              <span class="value">{{ formatDateTime(detail.endTime) }}</span>
         </div>
  <div class="info-item">
              <span class="label">用時:</span>
         <span class="value">{{ detail.useTime }}</span>
       </div>
     </div>
     </div>

<div v-if="detail.mapTicket || detail.compass.length > 0 || detail.probe" class="detail-section">
 <h3>使用材料</h3>
 <div class="materials">
    <div v-if="detail.mapTicket" class="material-item">
  <span class="material-icon">🎟️</span>
              <span>{{ detail.mapTicket }}</span>
   </div>
   <div v-for="(compass, index) in detail.compass" :key="index" class="material-item">
<span class="material-icon">🧭</span>
   <span>{{ compass }}</span>
   </div>
         <div v-if="detail.probe" class="material-item">
    <span class="material-icon">📍</span>
  <span>{{ detail.probe }}</span>
        </div>
        </div>
 </div>

<div v-if="detail.items && detail.items.length > 0" class="detail-section">
        <div class="items-grid">
      <div 
    v-for="item in detail.items" 
   :key="item.baseId"
       class="item-card"
  >
            <div class="item-name">{{ item.name }}</div>
    <div class="item-quantity">x{{ item.total }}</div>
     </div>
   </div>
      </div>

    <div v-else class="detail-section">
          <p class="empty-message">本次地圖未拾取任何物品</p>
     </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useMapStore } from '../stores/mapStore'

const router = useRouter()
const route = useRoute()
const mapStore = useMapStore()

const detail = ref(null)
const loading = ref(true)

onMounted(async () => {
  const recordId = route.params.id
  detail.value = await mapStore.getMapDetail(recordId)
  loading.value = false
})

const goBack = () => {
  router.push('/maps')
}

const formatDateTime = (dateStr) => {
  const date = new Date(dateStr)
  return date.toLocaleString('zh-TW', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}
</script>

<style scoped>
.map-detail {
  max-width: 1200px;
  margin: 0 auto;
}

.detail-header {
  margin-bottom: 20px;
}

.btn-back {
  padding: 10px 20px;
  background: rgba(255, 255, 255, 0.1);
border: 2px solid rgba(255, 255, 255, 0.2);
  border-radius: 8px;
  color: white;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-back:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: translateX(-3px);
}

.loading {
  text-align: center;
  padding: 60px;
  color: rgba(255, 255, 255, 0.7);
}

.detail-card {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  padding: 30px;
  color: white;
}

.detail-card h2 {
  margin: 0 0 30px 0;
  font-size: 2rem;
  border-bottom: 2px solid rgba(255, 255, 255, 0.2);
  padding-bottom: 15px;
}

.detail-section {
  margin-bottom: 30px;
}

.detail-section h3 {
  margin: 0 0 15px 0;
  color: rgba(255, 255, 255, 0.9);
  font-size: 1.3rem;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 15px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 15px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
}

.label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.9rem;
}

.value {
  color: white;
font-size: 1.1rem;
  font-weight: 500;
}

.materials {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.material-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 15px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 8px;
}

.material-icon {
  font-size: 1.5rem;
}

.items-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-top: 20px;
}

.item-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 12px 8px;
  background: rgba(76, 175, 80, 0.15);
  border: 1px solid rgba(76, 175, 80, 0.3);
  border-radius: 8px;
  transition: transform 0.2s, box-shadow 0.2s;
  min-height: 75px;
  overflow: hidden;
}

.item-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
}

.item-name {
  color: white;
  font-size: 0.9rem;
  text-align: center;
  line-height: 1.2;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
  width: 100%;
  padding: 0 4px;
}

.item-quantity {
  color: #4caf50;
  font-size: 1.1rem;
  font-weight: bold;
  white-space: nowrap;
  flex-shrink: 0;
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

.empty-message {
  text-align: center;
  padding: 40px;
  color: rgba(255, 255, 255, 0.5);
}
</style>
