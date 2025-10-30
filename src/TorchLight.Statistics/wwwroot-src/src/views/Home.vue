<template>
  <div class="home">
    <div class="welcome-card">
<h2>歡迎使用拾取物品統計工具</h2>
      <p>此工具會自動監控遊戲日誌，統計您在異界地圖中拾取的物品</p>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">???</div>
        <div class="stat-content">
    <div class="stat-label">總地圖數</div>
          <div class="stat-value">{{ mapStore.totalMaps }}</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon">??</div>
        <div class="stat-content">
        <div class="stat-label">總物品種類</div>
          <div class="stat-value">{{ mapStore.totalItems }}</div>
        </div>
</div>

      <div class="stat-card">
        <div class="stat-icon">?</div>
      <div class="stat-content">
          <div class="stat-label">狀態</div>
    <div class="stat-value status">{{ statusText }}</div>
      </div>
      </div>
    </div>

 <div class="quick-actions">
      <h3>快速操作</h3>
   <div class="action-buttons">
      <router-link to="/maps" class="action-btn">
          <span class="btn-icon">??</span>
          <span>查看地圖記錄</span>
        </router-link>
 
        <router-link to="/statistics" class="action-btn">
       <span class="btn-icon">??</span>
        <span>查看統計資料</span>
        </router-link>

        <button @click="exportRecords" class="action-btn">
          <span class="btn-icon">??</span>
          <span>匯出記錄</span>
        </button>

        <button @click="clearRecords" class="action-btn danger">
          <span class="btn-icon">???</span>
 <span>清除所有記錄</span>
 </button>
      </div>
 </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useMapStore } from '../stores/mapStore'

const mapStore = useMapStore()

const statusText = computed(() => {
  return mapStore.currentMapInfo.isInMap 
    ? `進行中: ${mapStore.currentMapInfo.mapName}` 
    : '待機中'
})

const exportRecords = async () => {
  const success = await mapStore.exportRecords()
  if (success) {
    alert('記錄已匯出')
  } else {
    alert('匯出失敗')
  }
}

const clearRecords = async () => {
  if (!confirm('確定要清除所有記錄嗎？此操作無法復原！')) {
    return
  }
  
const success = await mapStore.clearAllRecords()
  if (success) {
    alert('已清除所有記錄')
  } else {
    alert('清除失敗')
  }
}
</script>

<style scoped>
.home {
  max-width: 1200px;
  margin: 0 auto;
}

.welcome-card {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  padding: 30px;
  margin-bottom: 30px;
  text-align: center;
  color: white;
}

.welcome-card h2 {
  margin: 0 0 15px 0;
  font-size: 2rem;
}

.welcome-card p {
  margin: 0;
  font-size: 1.1rem;
  opacity: 0.8;
}

.stats-grid {
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
  transition: transform 0.3s, box-shadow 0.3s;
}

.stat-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.3);
}

.stat-icon {
  font-size: 3rem;
}

.stat-content {
  flex: 1;
}

.stat-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.9rem;
  margin-bottom: 5px;
}

.stat-value {
  color: white;
  font-size: 2rem;
  font-weight: 600;
}

.stat-value.status {
  font-size: 1.2rem;
}

.quick-actions {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  padding: 30px;
}

.quick-actions h3 {
  color: white;
  margin: 0 0 20px 0;
}

.action-buttons {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 15px;
}

.action-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 20px;
  background: rgba(255, 255, 255, 0.1);
  border: 2px solid rgba(255, 255, 255, 0.2);
  border-radius: 10px;
  color: white;
  text-decoration: none;
  cursor: pointer;
  transition: all 0.3s;
}

.action-btn:hover {
  background: rgba(255, 255, 255, 0.15);
  transform: translateY(-3px);
}

.action-btn.danger:hover {
  background: rgba(244, 67, 54, 0.3);
  border-color: #f44336;
}

.btn-icon {
  font-size: 2rem;
}
</style>
