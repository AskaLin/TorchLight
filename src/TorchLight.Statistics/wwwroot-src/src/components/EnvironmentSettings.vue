<template>
  <div class="environment-settings">
    <div class="settings-header">
      <h2>🔧 環境參數設定</h2>
      <p class="description">設定應用程式運行所需的環境參數</p>
    </div>

    <div class="settings-content">
      <!-- 遊戲日誌路徑設定 -->
      <div class="setting-section">
        <div class="section-header">
          <h3>📁 遊戲日誌檔案位置</h3>
          <p class="hint">請選擇遊戲日誌檔案 (UE_game.log)</p>
        </div>

        <div class="path-input-group">
          <input type="text"
                 v-model="gameLogPath"
                 placeholder="請選擇日誌檔案..."
                 readonly
                 class="path-input" />
          <button @click="selectFile" class="browse-btn">
            📄 選擇檔案
          </button>
        </div>

        <!-- 路徑狀態提示 -->
        <div v-if="gameLogPath" class="path-status">
          <span v-if="isPathValid" class="status-valid">
            ✅ 檔案路徑有效
          </span>
          <span v-else class="status-invalid">
            ❌ 檔案路徑無效或不存在
          </span>
        </div>

        <!-- 常見路徑提示 -->
        <div class="path-hints">
          <p class="hint-title">💡 常見日誌檔案位置：</p>
          <ul class="hint-list">
            <li>日誌基本路徑：<code>[遊戲安裝的資料夾]\Torchlight Infinite\Game\UE_game\TorchLight\Saved\Logs\UE_game.log</code></li>
            <li>Ex：<code>C:\Program Files (x86)\Torchlight Infinite\Game\UE_game\TorchLight\Saved\Logs\UE_game.log</code></li>
          </ul>
        </div>
      </div>

      <!-- 操作按鈕 -->
      <div class="action-buttons">
        <button @click="saveSettings" :disabled="!gameLogPath || saving" class="save-btn">
          <span v-if="!saving">💾 儲存設定</span>
          <span v-else>⏳ 儲存中...</span>
        </button>
        <button @click="resetSettings" class="reset-btn">
          🔄 重置
        </button>
      </div>

      <!-- 訊息提示 -->
      <div v-if="message" :class="['message', messageType]">
        {{ message }}
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted } from 'vue'
  import { apiCall } from '../utils/api'

  const gameLogPath = ref('')
  const isPathValid = ref(false)
  const saving = ref(false)
  const message = ref('')
  const messageType = ref('info') // 'success', 'error', 'info'

  // 載入現有設定
  onMounted(async () => {
    await loadSettings()
  })

  // 載入設定
  async function loadSettings() {
    try {
      const data = await apiCall('GetEnvironmentSettings')

      if (data.error) {
        showMessage('載入設定失敗：' + data.error, 'error')
        return
      }

      gameLogPath.value = data.gameLogPath || ''
      isPathValid.value = data.isConfigured || false
    } catch (error) {
      console.error('載入環境設定失敗:', error)
      showMessage('載入設定時發生錯誤', 'error')
    }
  }

  // 選擇檔案
  async function selectFile() {
    try {
      const data = await apiCall('OpenFileDialog', gameLogPath.value || '')
      if (data.success && data.path) {
        gameLogPath.value = data.path
        isPathValid.value = true
        showMessage('已選擇日誌檔案', 'info')        
      } else {
        showMessage(data.message, 'error')
      }

    } catch (error) {
      console.error('選擇檔案失敗:', error)
      showMessage('選擇檔案時發生錯誤', 'error')
    }
  }

  // 儲存設定
  async function saveSettings() {
    if (!gameLogPath.value) {
      showMessage('請先選擇遊戲日誌檔案', 'error')
      return
    }

    saving.value = true
    message.value = ''

    try {
      const data = await apiCall('SaveEnvironmentSettings', gameLogPath.value)

      if (data.success) {
        showMessage('✅ 設定已儲存成功！', 'success')
        isPathValid.value = true
      } else {
        showMessage('❌ ' + (data.message || '儲存失敗'), 'error')
      }
    } catch (error) {
      console.error('儲存環境設定失敗:', error)
      showMessage('儲存時發生錯誤', 'error')
    } finally {
      saving.value = false
    }
  }

  // 重置設定
  function resetSettings() {
    gameLogPath.value = ''
    isPathValid.value = false
    message.value = ''
  }

  // 顯示訊息
  function showMessage(msg, type = 'info') {
    message.value = msg
    messageType.value = type

    // 3 秒後自動清除訊息
    setTimeout(() => {
      message.value = ''
    }, 3000)
  }
</script>

<style scoped>
  .environment-settings {
    max-width: 1000px;
    margin: 0 auto;
  }

  .settings-header {
    margin-bottom: 30px;
  }

    .settings-header h2 {
      font-size: 1.8rem;
      color: white;
      margin-bottom: 10px;
    }

  .description {
    color: rgba(255, 255, 255, 0.7);
    font-size: 1rem;
  }

  .settings-content {
    background: rgba(255, 255, 255, 0.05);
    border-radius: 12px;
    padding: 30px;
    border: 1px solid rgba(255, 255, 255, 0.1);
  }

  .setting-section {
    margin-bottom: 30px;
  }

  .section-header {
    margin-bottom: 20px;
  }

    .section-header h3 {
      font-size: 1.3rem;
      color: white;
      margin-bottom: 8px;
    }

  .hint {
    color: rgba(255, 255, 255, 0.6);
    font-size: 0.9rem;
  }

  .path-input-group {
    display: flex;
    gap: 10px;
    margin-bottom: 15px;
  }

  .path-input {
    flex: 1;
    padding: 12px 16px;
    background: rgba(0, 0, 0, 0.3);
    border: 1px solid rgba(255, 255, 255, 0.2);
    border-radius: 8px;
    color: white;
    font-size: 1rem;
    cursor: default;
  }

    .path-input:focus {
      outline: none;
      border-color: #667eea;
    }

  .browse-btn {
    padding: 12px 24px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 1rem;
    font-weight: 500;
    transition: all 0.3s;
    white-space: nowrap;
  }

    .browse-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
    }

  .path-status {
    margin-bottom: 15px;
    padding: 10px;
    border-radius: 6px;
    font-size: 0.9rem;
  }

  .status-valid {
    color: #4ade80;
  }

  .status-invalid {
    color: #f87171;
  }

  .path-hints {
    background: rgba(0, 0, 0, 0.2);
    border-radius: 8px;
    padding: 15px;
    margin-top: 20px;
  }

  .hint-title {
    color: rgba(255, 255, 255, 0.8);
    margin-bottom: 10px;
    font-weight: 500;
  }

  .hint-list {
    list-style: none;
    padding: 0;
    margin: 0;
  }

    .hint-list li {
      color: rgba(255, 255, 255, 0.6);
      margin-bottom: 8px;
      font-size: 0.9rem;
      line-height: 1.6;
    }

    .hint-list code {
      background: rgba(255, 255, 255, 0.1);
      padding: 2px 8px;
      border-radius: 4px;
      color: #a5b4fc;
      font-family: 'Consolas', monospace;
      font-size: 0.85rem;
    }

  .action-buttons {
    display: flex;
    gap: 15px;
    margin-top: 30px;
  }

  .save-btn,
  .reset-btn {
    padding: 14px 32px;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 1rem;
    font-weight: 500;
    transition: all 0.3s;
  }

  .save-btn {
    background: linear-gradient(135deg, #10b981 0%, #059669 100%);
    color: white;
    flex: 1;
  }

    .save-btn:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 5px 15px rgba(16, 185, 129, 0.4);
    }

    .save-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .reset-btn {
    background: rgba(255, 255, 255, 0.1);
    color: white;
  }

    .reset-btn:hover {
      background: rgba(255, 255, 255, 0.15);
    }

  .message {
    margin-top: 20px;
    padding: 15px;
    border-radius: 8px;
    font-size: 1rem;
    animation: slideIn 0.3s ease-out;
  }

    .message.success {
      background: rgba(74, 222, 128, 0.2);
      border: 1px solid rgba(74, 222, 128, 0.4);
      color: #4ade80;
    }

    .message.error {
      background: rgba(248, 113, 113, 0.2);
      border: 1px solid rgba(248, 113, 113, 0.4);
      color: #f87171;
    }

    .message.info {
      background: rgba(165, 180, 252, 0.2);
      border: 1px solid rgba(165, 180, 252, 0.4);
      color: #a5b4fc;
    }

  @keyframes slideIn {
    from {
      opacity: 0;
      transform: translateY(-10px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
