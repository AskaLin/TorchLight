<template>
  <div class="execute-line-settings">
    <div class="settings-header">
      <h2>斬殺線設定</h2>
    </div>

    <!-- 通知訊息 -->
    <Transition name="notification-slide">
      <div v-if="notification.show"
           :class="['notification-float', notification.type]">
        {{ notification.message }}
      </div>
    </Transition>

    <!-- 主要內容區域 - 上下佈局 -->
    <div class="main-content">
      <!-- 上半部：預覽區域 -->
      <div class="preview-section">
        <label class="section-label">預覽</label>
        <div class="preview-container">
          <div class="preview-line">
            <!-- 第三階段：最左邊，從0開始 -->
            <div class="preview-stage preview-stage-3"
                 :style="{
      width: settings.stage3Percentage + '%',
        backgroundColor: settings.stage3Color,
      opacity: settings.opacity
        }">
            </div>
            <!-- 第二階段：中間 -->
            <div class="preview-stage preview-stage-2"
                 :style="{
   width: settings.stage2Percentage + '%',
 backgroundColor: settings.stage2Color,
       opacity: settings.opacity
         }">
            </div>
            <!-- 第一階段：預設區段右邊 -->
            <div class="preview-stage preview-stage-1"
                 :style="{
    width: settings.stage1Percentage + '%',
   backgroundColor: settings.stage1Color,
 opacity: settings.opacity
    }">
            </div>
            <!-- 預設區域：最右邊 -->
            <div class="preview-default"
                 :style="{
      width: remainingPercentage + '%',
     backgroundColor: settings.defaultColor,
   opacity: settings.opacity
       }">
            </div>
          </div>
          <div class="preview-labels">
            <span class="preview-label stage-3">階段3 {{ settings.stage3Percentage }}%</span>
            <span class="preview-label stage-2">階段2 {{ settings.stage2Percentage }}%</span>
            <span class="preview-label stage-1">階段1 {{ settings.stage1Percentage }}%</span>
            <span class="preview-label default">預設 {{ remainingPercentage }}%</span>
          </div>
        </div>
      </div>

      <!-- 下半部：設定區域 -->
      <div class="settings-section">
        <label class="section-label">設定</label>

        <div class="settings-panel">
          <!-- 第一列：預設區域顏色+透明度 | 階段3 -->
          <div class="settings-row">
            <!-- 左側：預設區域顏色 + 透明度 -->
            <div class="settings-group">
              <div class="settings-items-row">
                <div class="setting-item">
                  <label class="setting-label">預設區域</label>
                  <div class="control-group">
                    <input v-model="settings.defaultColor"
                           type="color"
                           class="color-input"
                           @input="updatePreview" />
                  </div>
                </div>
                <div class="setting-item">
                  <label class="setting-label">透明度</label>
                  <div class="control-group">
                    <input v-model.number="settings.opacity"
                           type="range"
                           min="0"
                           max="1"
                           step="0.01"
                           class="slider-input"
                           @input="updatePreview" />
                    <span class="slider-value">{{ Math.round(settings.opacity * 100) }}%</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- 右側：階段 3（最左邊，從0開始） -->
            <div class="settings-group">
              <div class="settings-items-row">
                <div class="setting-item">
                  <label class="setting-label">階段 3</label>
                  <div class="control-group">
                    <input v-model="settings.stage3Color"
                           type="color"
                           class="color-input"
                           @input="updatePreview" />
                  </div>
                </div>
                <div class="setting-item">
                  <label class="setting-label">百分比</label>
                  <div class="control-group">
                    <input v-model.number="settings.stage3Percentage"
                           type="range"
                           min="0"
                           max="100"
                           step="1"
                           class="slider-input"
                           @input="validateAndUpdate" />
                    <span class="slider-value">{{ settings.stage3Percentage }}%</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 第二列：階段2 | 階段1 -->
          <div class="settings-row">
            <!-- 左側：階段 2（中間） -->
            <div class="settings-group">
              <div class="settings-items-row">
                <div class="setting-item">
                  <label class="setting-label">階段 2</label>
                  <div class="control-group">
                    <input v-model="settings.stage2Color"
                           type="color"
                           class="color-input"
                           @input="updatePreview" />
                  </div>
                </div>
                <div class="setting-item">
                  <label class="setting-label">百分比</label>
                  <div class="control-group">
                    <input v-model.number="settings.stage2Percentage"
                           type="range"
                           min="0"
                           max="100"
                           step="1"
                           class="slider-input"
                           @input="validateAndUpdate" />
                    <span class="slider-value">{{ settings.stage2Percentage }}%</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- 右側：階段 1（預設區段右邊） -->
            <div class="settings-group">
              <div class="settings-items-row">
                <div class="setting-item">
                  <label class="setting-label">階段 1</label>
                  <div class="control-group">
                    <input v-model="settings.stage1Color"
                           type="color"
                           class="color-input"
                           @input="updatePreview" />
                  </div>
                </div>
                <div class="setting-item">
                  <label class="setting-label">百分比</label>
                  <div class="control-group">
                    <input v-model.number="settings.stage1Percentage"
                           type="range"
                           min="0"
                           max="100"
                           step="1"
                           class="slider-input"
                           @input="validateAndUpdate" />
                    <span class="slider-value">{{ settings.stage1Percentage }}%</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 儲存按鈕 -->
          <div class="save-button-container">
            <!-- 總和驗證警告 -->
            <Transition name="fade">
              <div v-if="isPercentageOverflow" class="validation-warning-inline">
                ⚠️ 百分比總和超過 100%
              </div>
            </Transition>
            <button @click="saveSettings" class="btn-save" :disabled="isPercentageOverflow">
              <span>💾 儲存設定</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { apiCall } from '../utils/api'

  const settings = ref({
    stage1Percentage: 20,
    stage1Color: '#FF0000',
    stage2Percentage: 15,
    stage2Color: '#FFA500',
    stage3Percentage: 15,
    stage3Color: '#FFFF00',
    defaultColor: '#00FF00',
    opacity: 0.95
  })

  const notification = ref({ show: false, type: 'success', message: '' })

  // 計算剩餘百分比
  const remainingPercentage = computed(() => {
    const total = settings.value.stage1Percentage +
      settings.value.stage2Percentage +
      settings.value.stage3Percentage
    return Math.max(0, 100 - total)
  })

  // 計算總和
  const totalStagePercentage = computed(() => {
    return settings.value.stage1Percentage +
      settings.value.stage2Percentage +
      settings.value.stage3Percentage
  })

  // 檢查是否超過 100%
  const isPercentageOverflow = computed(() => {
    return totalStagePercentage.value > 100
  })

  // 載入設定
  const loadSettings = async () => {
    try {
      const data = await apiCall('GetExecuteLineSettings')
      if (data) {
        settings.value.stage1Percentage = data.stage1Percentage || 20
        settings.value.stage1Color = data.stage1Color || '#FF0000'
        settings.value.stage2Percentage = data.stage2Percentage || 15
        settings.value.stage2Color = data.stage2Color || '#FFA500'
        settings.value.stage3Percentage = data.stage3Percentage || 15
        settings.value.stage3Color = data.stage3Color || '#FFFF00'
        settings.value.defaultColor = data.defaultColor || '#00FF00'
        settings.value.opacity = data.opacity !== undefined ? data.opacity : 0.95
      }
    } catch (err) {
      console.error('載入斬殺線設定失敗:', err)
      showNotification('error', '載入設定失敗: ' + err.message)
    }
  }

  // 驗證並更新
  const validateAndUpdate = () => {
    // 如果超過 100%，顯示警告但仍允許調整
    updatePreview()
  }

  // 儲存設定
  const saveSettings = async () => {
    if (isPercentageOverflow.value) {
      showNotification('error', '三階段百分比總和不能超過 100%')
      return
    }

    try {
      const result = await apiCall(
        'SaveExecuteLineSettings',
        settings.value.stage1Percentage,
        settings.value.stage1Color,
        settings.value.stage2Percentage,
        settings.value.stage2Color,
        settings.value.stage3Percentage,
        settings.value.stage3Color,
        settings.value.defaultColor,
        settings.value.opacity
      )

      if (result.success) {
        showNotification('success', '設定已儲存')
      } else {
        showNotification('error', result.message || '儲存失敗')
      }
    } catch (err) {
      showNotification('error', '儲存失敗: ' + err.message)
    }
  }

  // 更新預覽
  const updatePreview = async () => {
    try {
      await apiCall(
        'UpdateExecuteLinePreview',
        settings.value.stage1Percentage,
        settings.value.stage1Color,
        settings.value.stage2Percentage,
        settings.value.stage2Color,
        settings.value.stage3Percentage,
        settings.value.stage3Color,
        settings.value.defaultColor,
        settings.value.opacity
      )
    } catch (err) {
      console.error('更新預覽失敗:', err)
    }
  }

  // 顯示通知
  const showNotification = (type, message) => {
    notification.value = { show: true, type, message }
    setTimeout(() => {
      notification.value.show = false
    }, 3000)
  }

  onMounted(async () => {
    await loadSettings()
  })
</script>

<style scoped>
  .execute-line-settings {
    width: 100%;
    max-width: 900px;
    margin: 0 auto;
  }

  .settings-header {
    margin-bottom: 25px;
  }

    .settings-header h2 {
      color: white;
      margin: 0;
    }

  /* 主要內容區域 - 上下佈局 */
  .main-content {
    display: flex;
    flex-direction: column;
    gap: 30px;
  }

  /* 區塊標籤 */
  .section-label {
    display: block;
    color: white;
    font-size: 1.2rem;
    font-weight: 600;
    margin-bottom: 15px;
  }

  /* 通知訊息 */
  .notification-float {
    position: fixed;
    top: 20px;
    left: 50%;
    transform: translateX(-50%);
    z-index: 9999;
    padding: 15px 30px;
    border-radius: 8px;
    font-weight: 500;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
    backdrop-filter: blur(10px);
    min-width: 300px;
    max-width: 500px;
    text-align: center;
  }

    .notification-float.success {
      background: rgba(76, 175, 80, 0.95);
      border: 1px solid #4caf50;
      color: white;
    }

    .notification-float.error {
      background: rgba(244, 67, 54, 0.95);
      border: 1px solid #f44336;
      color: white;
    }

  .notification-slide-enter-active {
    animation: slideInDown 0.3s ease-out;
  }

  .notification-slide-leave-active {
    animation: slideOutUp 0.3s ease-in;
  }

  @keyframes slideInDown {
    from {
      opacity: 0;
      transform: translateX(-50%) translateY(-20px);
    }

    to {
      opacity: 1;
      transform: translateX(-50%) translateY(0);
    }
  }

  @keyframes slideOutUp {
    from {
      opacity: 1;
      transform: translateX(-50%) translateY(0);
    }

    to {
      opacity: 0;
      transform: translateX(-50%) translateY(-20px);
    }
  }

  /* 預覽區域 */
  .preview-section {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border: 2px solid rgba(255, 255, 255, 0.1);
    border-radius: 12px;
    padding: 30px;
  }

  .preview-container {
    background: rgba(0, 0, 0, 0.3);
    border-radius: 8px;
    padding: 20px;
  }

  .preview-line {
    height: 50px;
    display: flex;
    border-radius: 8px;
    overflow: hidden;
    border: 2px solid rgba(255, 255, 255, 0.3);
    margin-bottom: 15px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  }

  .preview-stage,
  .preview-default {
    height: 100%;
    transition: width 0.3s ease-out;
  }

  .preview-labels {
    display: flex;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 10px;
  }

  .preview-label {
    font-size: 0.9rem;
    font-weight: 500;
    padding: 4px 12px;
    border-radius: 4px;
    background: rgba(0, 0, 0, 0.2);
  }

    .preview-label.stage-1 {
      color: #ff6b6b;
    }

    .preview-label.stage-2 {
      color: #ffa94d;
    }

    .preview-label.stage-3 {
      color: #ffd43b;
    }

    .preview-label.default {
      color: #51cf66;
    }

  /* 設定區域 */
  .settings-section {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border: 2px solid rgba(255, 255, 255, 0.1);
    border-radius: 12px;
    padding: 30px;
  }

  .settings-panel {
    display: flex;
    flex-direction: column;
    gap: 25px;
  }

  .fade-enter-active,
  .fade-leave-active {
    transition: opacity 0.3s;
  }

  .fade-enter-from,
  .fade-leave-to {
    opacity: 0;
  }

  /* 設定列 - 兩欄布局 */
  .settings-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
  }

  /* 設定群組 */
  .settings-group {
    background: rgba(0, 0, 0, 0.2);
    border-radius: 8px;
    padding: 15px;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  /* 階段標題 */
  .stage-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
    padding-bottom: 8px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  }

  .stage-name {
    color: white;
    font-weight: 600;
    font-size: 1rem;
  }

  .stage-percentage {
    color: rgba(255, 255, 255, 0.8);
    font-family: 'Consolas', monospace;
    font-size: 1rem;
    font-weight: 600;
  }

  /* 設定項目並列容器 */
  .settings-items-row {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 15px;
  }

  /* 設定項目 */
  .setting-item {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .setting-label {
    color: rgba(255, 255, 255, 0.9);
    font-size: 0.85rem;
    font-weight: 500;
    text-align: center;
  }

  /* 控制組 */
  .control-group {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  /* 顏色選擇器 */
  .color-input {
    width: 60px;
    height: 36px;
    border: 2px solid rgba(255, 255, 255, 0.2);
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.3s;
  }

    .color-input:hover {
      border-color: #667eea;
      transform: scale(1.05);
    }

  .color-value {
    color: rgba(255, 255, 255, 0.8);
    font-family: 'Consolas', monospace;
    font-size: 0.85rem;
    flex: 1;
  }

  /* 滑桿 */
  .slider-input {
    flex: 1;
    height: 6px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 3px;
    outline: none;
    -webkit-appearance: none;
    cursor: pointer;
  }

    .slider-input::-webkit-slider-thumb {
      -webkit-appearance: none;
      appearance: none;
      width: 18px;
      height: 18px;
      border-radius: 50%;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      cursor: pointer;
      box-shadow: 0 2px 8px rgba(102, 126, 234, 0.5);
      transition: all 0.3s;
    }

      .slider-input::-webkit-slider-thumb:hover {
        transform: scale(1.2);
        box-shadow: 0 3px 10px rgba(102, 126, 234, 0.7);
      }

    .slider-input::-moz-range-thumb {
      width: 18px;
      height: 18px;
      border-radius: 50%;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      cursor: pointer;
      border: none;
      box-shadow: 0 2px 8px rgba(102, 126, 234, 0.5);
      transition: all 0.3s;
    }

      .slider-input::-moz-range-thumb:hover {
        transform: scale(1.2);
        box-shadow: 0 3px 10px rgba(102, 126, 234, 0.7);
      }

  .slider-value {
    color: rgba(255, 255, 255, 0.8);
    font-family: 'Consolas', monospace;
    font-size: 0.85rem;
    min-width: 45px;
    text-align: right;
  }

  /* 儲存按鈕容器 */
  .save-button-container {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 15px;
    padding-top: 10px;
    position: relative;
    min-height: 48px;
  }

  /* 內聯驗證警告 */
  .validation-warning-inline {
    background: rgba(244, 67, 54, 0.2);
    border: 2px solid #f44336;
    border-radius: 8px;
    padding: 8px 16px;
    color: #ff6b6b;
    font-weight: 500;
    font-size: 0.85rem;
    white-space: nowrap;
    width: 100%;
    height: 46px;
    margin-top: 3px;
  }

  .btn-save {
    padding: 12px 32px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border: none;
    border-radius: 8px;
    color: white;
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.3s;
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    flex-shrink: 0;
  }

    .btn-save:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(102, 126, 234, 0.6);
    }

    .btn-save:active:not(:disabled) {
      transform: translateY(0);
    }

    .btn-save:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  /* 響應式設計 */
  @media (max-width: 768px) {
    .settings-row {
      grid-template-columns: 1fr;
    }

    .settings-items-row {
      grid-template-columns: 1fr;
    }

    .preview-labels {
      flex-direction: column;
    }
  }
</style>
