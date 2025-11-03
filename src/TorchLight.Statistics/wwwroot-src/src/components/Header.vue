<template>
  <header class="header">
    <div class="header-content">
      <div class="logo">
        <h1>🔥 火炬之光無限 - 拾取統計</h1>
      </div>

      <nav class="nav">
        <router-link to="/" class="nav-link">首頁</router-link>
        <router-link to="/maps" class="nav-link">地圖記錄</router-link>
        <router-link to="/statistics" class="nav-link">統計</router-link>
        <router-link to="/history" class="nav-link">📚 歷史紀錄</router-link>
        <router-link to="/settings" class="nav-link">⚙️ 設定</router-link>
      </nav>

      <div class="status">
        <div v-if="currentMapInfo.isInMap" class="status-indicator online">
          <span class="dot"></span>
          <span class="status-text">進行中: {{ currentMapInfo.mapName }}</span>
          <button @click="settleMap" class="btn-settle" title="結算地圖" :disabled="isSettling">
            {{ isSettling ? '結算中...' : '💰 結算' }}
          </button>
        </div>
        <div v-else class="status-indicator offline">
          <span class="dot"></span>
          待機中
        </div>
      </div>

      <div class="actions">
        <!-- 浮動窗體控制按鈕 -->
        <button @click="toggleFloatingWindow" class="btn-icon btn-float" :title="floatingWindowVisible ? '隱藏浮動窗體' : '顯示浮動窗體'">
          <span>{{ floatingWindowVisible ? '📊' : '📉' }}</span>
        </button>

        <!-- 斬殺線控制按鈕 -->
        <button @click="toggleExecuteLine" class="btn-icon btn-execute" :title="executeLineVisible ? '隱藏斬殺線' : '顯示斬殺線'">
          <span>{{ executeLineVisible ? '⚔️' : '🗡️' }}</span>
        </button>

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
  import { computed, ref, onMounted } from 'vue'
  import { useMapStore } from '../stores/mapStore'
  import { apiCall } from '../utils/api'

  const mapStore = useMapStore()
  const currentMapInfo = computed(() => mapStore.currentMapInfo)
  const isSettling = ref(false)
  const floatingWindowVisible = ref(true)
  const executeLineVisible = ref(false)

  const minimizeWindow = () => {
    apiCall('MinimizeWindow').catch(console.error)
  }

  const closeWindow = () => {
    if (confirm('確定要關閉應用程式嗎？')) {
      apiCall('CloseApplication').catch(console.error)
    }
  }

  const settleMap = async () => {
    if (isSettling.value) return

    if (!confirm(`確定要結算地圖「${currentMapInfo.value.mapName}」嗎？`)) {
      return
    }

    isSettling.value = true

    try {
      const result = await apiCall('SettleCurrentMap')

      if (result && result.success) {
        // 結算成功後，重新載入地圖資訊和地圖記錄
        await mapStore.refreshCurrentMap()
        await mapStore.refreshRecords()

        alert(`結算成功！\n地圖：${result.mapName}\n已儲存拾取記錄`)
      } else {
        alert(`結算失敗：${result?.message || '未知錯誤'}`)
      }
    } catch (error) {
      console.error('結算地圖時發生錯誤:', error)
      alert(`結算失敗：${error.message}`)
    } finally {
      isSettling.value = false
    }
  }

  // 切換浮動窗體顯示
  const toggleFloatingWindow = async () => {
    try {
      const result = await apiCall('ToggleFloatingStatsWindow')

      if (result && result.success) {
        floatingWindowVisible.value = result.isVisible
        console.log(result.message)
      } else {
        console.error('切換浮動窗體失敗:', result?.message)
      }
    } catch (error) {
      console.error('切換浮動窗體時發生錯誤:', error)
    }
  }

  // 切換斬殺線顯示
  const toggleExecuteLine = async () => {
    try {
      const result = await apiCall('ToggleExecuteLineWindow')

      if (result && result.success) {
        executeLineVisible.value = result.isVisible
        console.log(result.message)
      } else {
        console.error('切換斬殺線失敗:', result?.message)
      }
    } catch (error) {
      console.error('切換斬殺線時發生錯誤:', error)
    }
  }

  // 啟動時向後端查詢斬殺線設定（包含 isVisible）
  onMounted(async () => {
    try {
      const result = await apiCall('GetExecuteLineSettings')
      if (result && typeof result.isVisible !== 'undefined') {
        console.log('載入斬殺線設定', result)
        executeLineVisible.value = result.isVisible
      }
    } catch (err) {
      console.error('載入斬殺線設定失敗:', err)
    }
  })
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

  .status-text {
    display: inline-block;
  }

  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: currentColor;
    animation: pulse 2s ease-in-out infinite;
  }

  @keyframes pulse {
    0%, 100% {
      opacity: 1;
    }

    50% {
      opacity: 0.5;
    }
  }

  /* 結算按鈕 */
  .btn-settle {
    padding: 4px 12px;
    border: none;
    background: linear-gradient(135deg, #ffd700 0%, #ffed4e 100%);
    color: #333;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.85rem;
    font-weight: 600;
    transition: all 0.3s;
    white-space: nowrap;
  }

    .btn-settle:hover:not(:disabled) {
      background: linear-gradient(135deg, #ffed4e 0%, #ffd700 100%);
      transform: translateY(-1px);
      box-shadow: 0 2px 8px rgba(255, 215, 0, 0.4);
    }

    .btn-settle:active:not(:disabled) {
      transform: translateY(0);
    }

    .btn-settle:disabled {
      opacity: 0.6;
      cursor: not-allowed;
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

  /* 浮動窗體按鈕特殊樣式 */
  .btn-float {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  }

    .btn-float:hover {
      background: linear-gradient(135deg, #764ba2 0%, #667eea 100%);
      transform: scale(1.1);
    }

  /* 斬殺線按鈕特殊樣式 */
  .btn-execute {
    background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%);
  }

    .btn-execute:hover {
      background: linear-gradient(135deg, #ee5a6f 0%, #ff6b6b 100%);
      transform: scale(1.1);
    }

  .btn-close:hover {
    background: #f44336;
  }
</style>
