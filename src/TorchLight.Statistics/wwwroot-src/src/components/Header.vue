<template>
  <header class="header">
    <div class="header-content">
      <div class="logo">
        <h1>?? 火炬之光無限 - 拾取統計</h1>
      </div>
 
      <nav class="nav">
        <router-link to="/" class="nav-link">首頁</router-link>
        <router-link to="/maps" class="nav-link">地圖記錄</router-link>
      <router-link to="/statistics" class="nav-link">統計</router-link>
      </nav>

      <div class="status">
        <div v-if="currentMapInfo.isInMap" class="status-indicator online">
          <span class="dot"></span>
     進行中: {{ currentMapInfo.mapName }}
   </div>
 <div v-else class="status-indicator offline">
          <span class="dot"></span>
       待機中
        </div>
      </div>
  
      <div class="actions">
      <button @click="minimizeWindow" class="btn-icon" title="最小化">
      <span>-</span>
 </button>
<button @click="closeWindow" class="btn-icon btn-close" title="關閉">
          <span>×</span>
        </button>
      </div>
    </div>
  </header>
</template>

<script setup>
import { computed } from 'vue'
import { useMapStore } from '../stores/mapStore'
import { apiCall } from '../utils/api'

const mapStore = useMapStore()
const currentMapInfo = computed(() => mapStore.currentMapInfo)

const minimizeWindow = () => {
  apiCall('MinimizeWindow').catch(console.error)
}

const closeWindow = () => {
  if (confirm('確定要關閉應用程式嗎？')) {
    apiCall('CloseApplication').catch(console.error)
  }
}
</script>

<style scoped>
.header {
  background: linear-gradient(135deg, #0f0c29 0%, #302b63 50%, #24243e 100%);
  color: white;
  padding: 0 20px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
  -webkit-app-region: drag;
}

.header-content {
  display: flex;
  align-items: center;
  height: 60px;
  gap: 20px;
}

.logo h1 {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 600;
}

.nav {
  display: flex;
  gap: 10px;
  flex: 1;
  -webkit-app-region: no-drag;
}

.nav-link {
  padding: 8px 16px;
  color: rgba(255, 255, 255, 0.8);
  text-decoration: none;
  border-radius: 6px;
  transition: all 0.3s;
}

.nav-link:hover {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.nav-link.router-link-active {
background: rgba(255, 255, 255, 0.2);
  color: white;
}

.status {
  -webkit-app-region: no-drag;
}

.status-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 0.9rem;
}

.status-indicator.online {
  background: rgba(76, 175, 80, 0.2);
  color: #4caf50;
}

.status-indicator.offline {
  background: rgba(158, 158, 158, 0.2);
  color: #9e9e9e;
}

.dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: currentColor;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.actions {
  display: flex;
  gap: 8px;
  -webkit-app-region: no-drag;
}

.btn-icon {
  width: 32px;
  height: 32px;
  border: none;
  background: rgba(255, 255, 255, 0.1);
  color: white;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.2rem;
}

.btn-icon:hover {
  background: rgba(255, 255, 255, 0.2);
}

.btn-close:hover {
  background: #f44336;
}
</style>
