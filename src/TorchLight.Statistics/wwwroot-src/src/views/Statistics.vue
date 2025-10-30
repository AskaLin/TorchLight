<template>
  <div class="statistics">
    <h2>統計資料</h2>

    <div v-if="loading" class="loading">載入中...</div>

    <div v-else-if="stats" class="stats-container">
     <div class="stats-overview">
   <div class="stat-card">
          <div class="stat-icon">???</div>
  <div class="stat-content">
     <div class="stat-value">{{ stats.totalMaps }}</div>
      <div class="stat-label">總地圖數</div>
     </div>
      </div>

    <div class="stat-card">
   <div class="stat-icon">??</div>
      <div class="stat-content">
      <div class="stat-value">{{ stats.totalItems }}</div>
  <div class="stat-label">總物品種類</div>
    </div>
        </div>

    <div class="stat-card">
       <div class="stat-icon">??</div>
      <div class="stat-content">
 <div class="stat-value">{{ stats.totalQuantity }}</div>
       <div class="stat-label">總拾取數量</div>
    </div>
   </div>

    <div class="stat-card">
<div class="stat-icon">??</div>
    <div class="stat-content">
     <div class="stat-value">{{ stats.totalPlayTime }}</div>
   <div class="stat-label">總遊戲時間</div>
       </div>
        </div>
      </div>

   <div v-if="stats.mostPickedItems && stats.mostPickedItems.length > 0" class="most-picked">
        <h3>最常拾取物品 Top 10</h3>
     <div class="items-list">
     <div 
          v-for="(item, index) in stats.mostPickedItems" 
            :key="item.baseId"
  class="item-row"
  >
      <div class="rank">{{ index + 1 }}</div>
       <div class="item-name">{{ item.name }}</div>
   <div class="item-quantity">{{ item.totalQuantity }}</div>
          </div>
        </div>
</div>
  </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { apiCall } from '../utils/api'

const stats = ref(null)
const loading = ref(true)

onMounted(async () => {
  try {
    stats.value = await apiCall('GetStatistics')
  } catch (err) {
  console.error('Failed to load statistics:', err)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.statistics {
  max-width: 1200px;
  margin: 0 auto;
}

.statistics h2 {
  color: white;
  margin: 0 0 25px 0;
}

.loading {
  text-align: center;
  padding: 60px;
  color: rgba(255, 255, 255, 0.7);
}

.stats-overview {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  padding: 25px;
  display: flex;
  align-items: center;
  gap: 20px;
  transition: transform 0.3s;
}

.stat-card:hover {
  transform: translateY(-5px);
}

.stat-icon {
  font-size: 3rem;
}

.stat-content {
  flex: 1;
}

.stat-value {
  color: white;
  font-size: 2.5rem;
  font-weight: 700;
  margin-bottom: 5px;
}

.stat-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.95rem;
}

.most-picked {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  padding: 25px;
}

.most-picked h3 {
  color: white;
  margin: 0 0 20px 0;
  font-size: 1.5rem;
}

.items-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.item-row {
  display: grid;
  grid-template-columns: 50px 1fr 150px;
  align-items: center;
  padding: 15px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
  transition: all 0.3s;
}

.item-row:hover {
  background: rgba(255, 255, 255, 0.1);
  transform: translateX(5px);
}

.rank {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 50%;
  color: white;
  font-weight: 700;
  font-size: 1.2rem;
}

.item-name {
  color: white;
  font-size: 1.1rem;
}

.item-quantity {
  text-align: right;
  color: #4caf50;
  font-size: 1.5rem;
  font-weight: 600;
}
</style>
