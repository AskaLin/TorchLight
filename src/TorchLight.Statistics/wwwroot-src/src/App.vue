<template>
  <div id="app" class="app-container">
    <!-- 設定提示模態框 -->
    <div v-if="showConfigPrompt" class="config-prompt-overlay">
      <div class="config-prompt-modal">
        <div class="prompt-icon">⚠️</div>
        <h2>需要進行初始設定</h2>
        <p>請先設定遊戲日誌存放位置，才能開始使用本工具</p>
        <div class="prompt-buttons">
          <button @click="goToSettings" class="primary-btn">
            前往設定
          </button>
        </div>
      </div>
    </div>

    <Header />
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import Header from './components/Header.vue'
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useMapStore } from './stores/mapStore'
import { apiCall } from './utils/api'

const router = useRouter()
const mapStore = useMapStore()
const showConfigPrompt = ref(false)

onMounted(async () => {
  // 檢查環境設定
  await checkEnvironmentConfig()

  // 設定全域事件處理器
  window.onNewMapRecord = () => {
    console.log('New map record detected')
    mapStore.refreshRecords()
  }

  window.onItemPicked = (itemName, quantity) => {
    console.log(`Item picked: ${itemName} x${quantity}`)
    mapStore.refreshCurrentMap()
  }

  // 初始載入資料
  mapStore.refreshRecords()
  mapStore.refreshCurrentMap()
})

// 檢查環境設定
async function checkEnvironmentConfig() {
  try {
    const data = await apiCall('GetEnvironmentSettings')

    // 如果未設定或設定無效，顯示提示
    if (!data.isConfigured) {
      showConfigPrompt.value = true
    }
  } catch (error) {
    console.error('檢查環境設定失敗:', error)
  }
}

// 前往設定頁面
function goToSettings() {
  showConfigPrompt.value = false
  router.push({ name: 'settings' })
}
</script>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
}

.main-content {
  flex: 1;
  overflow: auto;
  padding: 20px;
}

/* 設定提示模態框樣式 */
.config-prompt-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  animation: fadeIn 0.3s ease-out;
}

.config-prompt-modal {
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
  border-radius: 16px;
  padding: 40px;
  max-width: 500px;
  width: 90%;
  text-align: center;
  border: 2px solid rgba(102, 126, 234, 0.3);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
  animation: slideUp 0.3s ease-out;
}

.prompt-icon {
  font-size: 4rem;
  margin-bottom: 20px;
  animation: pulse 2s infinite;
}

.config-prompt-modal h2 {
  color: white;
  font-size: 1.8rem;
  margin-bottom: 15px;
}

.config-prompt-modal p {
  color: rgba(255, 255, 255, 0.7);
  font-size: 1.1rem;
  line-height: 1.6;
  margin-bottom: 30px;
}

.prompt-buttons {
  display: flex;
  justify-content: center;
  gap: 15px;
}

.primary-btn {
  padding: 14px 32px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 1.1rem;
  font-weight: 500;
  transition: all 0.3s;
}

.primary-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes pulse {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.1);
  }
}
</style>
