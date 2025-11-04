<template>
  <div class="map-settings-panel">
    <!-- 設定標題與新增按鈕 -->
    <div class="settings-header">
      <h2>地圖設定管理</h2>
      <button @click="showAddMapDialog = true" class="btn-add">
        <span>➕ 新增地圖</span>
      </button>
    </div>

    <!-- ✅ 通知訊息組件 -->
    <Notification :show="notification.show"
                  :type="notification.type"
                  :message="notification.message" />

    <!-- 地圖列表 -->
    <CollapsibleList :sections="formattedSections">
    <!-- 項目卡片 slot -->
      <template #item="{ items }">
        <div v-for="map in items"
             :key="map.name"
             :class="['map-card', getTypeClass(map.type)]">
        <div class="card-header">
            <div class="map-name">{{ map.name }}</div>
            <div class="card-actions">
          <button @click.stop="editMap(map)" class="btn-icon" title="編輯">
 ✏️
        </button>
              <button @click.stop="deleteMap(map)" class="btn-icon" title="刪除">
            🗑️
      </button>
  </div>
          </div>
          <div class="card-footer">
       <span class="map-ids">{{ formatMapIds(map.mapIds) }}</span>
          </div>
        </div>
      </template>
    </CollapsibleList>

    <!-- 地圖新增/編輯對話框 -->
    <div v-if="showAddMapDialog" class="modal-overlay" @click.self="closeMapDialog">
      <div class="modal-content">
        <div class="modal-header">
   <h3>{{ isEditingMap ? '編輯地圖' : '新增地圖' }}</h3>
          <button @click="closeMapDialog" class="btn-close">✕</button>
        </div>

        <div class="modal-body">
 <div class="form-group">
    <label>地圖名稱 *</label>
            <input v-model="editingMap.name"
     type="text"
    placeholder="例如: 雜蕪街區"
      class="form-input" />
            <div class="form-hint">輸入地圖名稱（不包含等級前綴）</div>
        </div>

          <div class="form-group">
            <label>地圖 ID 列表 *</label>
<textarea v-model="editingMap.mapIdsText"
      placeholder="輸入地圖 ID，每行一個&#10;例如:&#10;1061000&#10;1071000&#10;1081000"
           class="form-textarea"
            rows="6"></textarea>
       <div class="form-hint">每行輸入一個地圖 ID，系統會自動為這些 ID 套用相同名稱</div>
          </div>

          <div class="form-group">
       <label>地圖類型 *</label>
  <select v-model="editingMap.type" class="form-select">
         <option v-for="type in mapTypes"
          :key="type.value"
:value="type.value">
      {{ type.name }}
      </option>
   </select>
       <div v-if="mapTypes.length === 0" class="form-hint">
        載入地圖類型中...
     </div>
  </div>
      </div>

      <div class="modal-footer">
       <button @click="closeMapDialog" class="btn-cancel">取消</button>
       <button @click="saveMap" class="btn-save">儲存</button>
</div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { apiCall } from '../utils/api'
  import { useNotification } from '../composables/useNotification'
  import CollapsibleList from './CollapsibleList.vue'
  import Notification from './Notification.vue'

  const { notification, showNotification } = useNotification()

  const maps = ref([])
  const mapTypes = ref([])
  const showAddMapDialog = ref(false)
  const isEditingMap = ref(false)

  const editingMap = ref({
    name: '',
    mapIdsText: '',
    type: 'Netherrealm'
  })

  // 格式化資料給 CollapsibleList
  const formattedSections = computed(() => {
  const sections = []

    // 已知地圖類型
    mapTypes.value.forEach(type => {
      const items = maps.value.filter(map => map.type === type.value)
      sections.push({
        key: type.value,
        name: type.name,
        totalCount: items.length,
        items: items
      })
    })

    // 未知地圖（未分類）
    const unknownMaps = maps.value.filter(map => !map.type)
    if (unknownMaps.length > 0) {
      sections.push({
        key: 'Unknown',
        name: '❓ 未分類地圖',
        totalCount: unknownMaps.length,
  items: unknownMaps
      })
    }

    return sections
  })

  // 格式化 MapIds 顯示
  const formatMapIds = (mapIds) => {
    if (!mapIds || mapIds.length === 0) return ''
    if (mapIds.length <= 3) {
   return `ID: ${mapIds.join(', ')}`
    }
    return `ID: ${mapIds.slice(0, 3).join(', ')}... (共 ${mapIds.length} 個)`
  }

  // 關閉對話框
  const closeMapDialog = () => {
 showAddMapDialog.value = false
    isEditingMap.value = false
    editingMap.value = {
      name: '',
      mapIdsText: '',
      type: mapTypes.value.length > 0 ? mapTypes.value[0].value : 'Netherrealm'
    }
  }

  // 儲存地圖
  const saveMap = async () => {
    if (!editingMap.value.name) {
      showNotification('error', '地圖名稱是必填的')
      return
    }

    if (!editingMap.value.mapIdsText.trim()) {
      showNotification('error', '至少需要輸入一個地圖 ID')
      return
    }

    // 解析 MapIds
    const mapIds = editingMap.value.mapIdsText
      .split('\n')
      .map(line => line.trim())
      .filter(line => line !== '')
 .map(line => parseInt(line))
      .filter(id => !isNaN(id) && id > 0)

    if (mapIds.length === 0) {
      showNotification('error', '沒有有效的地圖 ID')
      return
    }

    try {
      const result = await apiCall(
     'SaveMapConfig',
        editingMap.value.name,
  JSON.stringify(mapIds),
        editingMap.value.type
      )

      if (result.success) {
    showNotification('success', result.message)
    closeMapDialog()
    await loadMaps()
      } else {
        showNotification('error', result.message)
      }
    } catch (error) {
      showNotification('error', '儲存地圖時發生錯誤: ' + error.message)
    }
  }

  // 載入地圖資料
  const loadMaps = async () => {
    try {
   await loadMapTypes()

    const configs = await apiCall('GetMapConfigs')

maps.value = []
      for (const [mapType, mapList] of Object.entries(configs)) {
        for (const map of mapList) {
          maps.value.push({
            name: map.name,
            mapIds: map.mapIds || [],
            type: map.type
   })
        }
      }
    } catch (error) {
      showNotification('error', '載入地圖資料時發生錯誤: ' + error.message)
    }
  }

  // 載入地圖類型
  const loadMapTypes = async () => {
    try {
      const types = await apiCall('GetMapTypes')
      mapTypes.value = types
    } catch (error) {
    console.error('載入地圖類型失敗:', error)
      showNotification('error', '載入地圖類型失敗: ' + error.message)
    }
  }

  // 編輯地圖
  const editMap = (map) => {
    editingMap.value = {
      name: map.name,
      mapIdsText: map.mapIds.join('\n'),
      type: map.type
    }
    isEditingMap.value = true
    showAddMapDialog.value = true
  }

  // 刪除地圖
  const deleteMap = async (map) => {
    if (!confirm(`確定要刪除地圖「${map.name}」嗎？\n這將會刪除 ${map.mapIds.length} 個相關的地圖 ID。`)) {
      return
    }

    try {
      const result = await apiCall('DeleteMapConfig', map.name)

if (result.success) {
        showNotification('success', result.message)
    await loadMaps()
      } else {
     showNotification('error', result.message)
      }
    } catch (error) {
      showNotification('error', '刪除地圖時發生錯誤: ' + error.message)
    }
  }

  const getTypeClass = (type) => {
    const typeMap = {
      Netherrealm: 'netherrealm',
      Hideout: 'hideout',
      SecretRealm: 'secret-realm',
      Boss: 'boss',
      Unknown: 'unknown'
    }
    return typeMap[type] || 'unknown'
  }

  // 初始化
  onMounted(async () => {
    await loadMaps()
  })
</script>

<style scoped>
  .map-settings-panel {
    width: 100%;
  }

  .settings-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 25px;
  }

    .settings-header h2 {
      color: white;
      margin: 0;
  }

  .btn-add {
padding: 10px 20px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border: none;
    border-radius: 8px;
    color: white;
    font-size: 1rem;
    cursor: pointer;
    transition: all 0.3s;
  }

    .btn-add:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

  /* 通知 */
  .notification {
    padding: 15px 20px;
    border-radius: 8px;
    margin-bottom: 20px;
    font-weight: 500;
    animation: slideIn 0.3s ease-out;
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

  .notification.success {
    background: rgba(76, 175, 80, 0.2);
    border: 1px solid #4caf50;
  color: #4caf50;
  }

  .notification.error {
  background: rgba(244, 67, 54, 0.2);
    border: 1px solid #f44336;
    color: #f44336;
  }

  /* 地圖卡片 */
  .map-card {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border: 2px solid rgba(255, 255, 255, 0.1);
    border-radius: 12px;
    padding: 20px;
    transition: all 0.3s;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

    .map-card:hover {
      border-color: rgba(255, 255, 255, 0.3);
      transform: translateY(-5px);
      box-shadow: 0 8px 20px rgba(0, 0, 0, 0.3);
}

  /* 卡片頭部 */
  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 10px;
    padding-bottom: 12px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  }

  .map-name {
    color: white;
    font-size: 1.1rem;
    font-weight: 600;
    word-break: break-word;
    flex: 1;
  }

  .card-actions {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
  }

  /* 卡片底部 */
  .card-footer {
    display: flex;
    align-items: center;
  }

  .map-ids {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.6);
  }

  .btn-icon {
    width: 32px;
    height: 32px;
    border: none;
    border-radius: 6px;
    font-size: 1.1rem;
    cursor: pointer;
    transition: all 0.3s;
    background: rgba(255, 255, 255, 0.1);
  }

    .btn-icon:hover {
      background: rgba(255, 255, 255, 0.2);
      transform: scale(1.1);
    }

  /* 地圖類型顏色 */
  .map-card.netherrealm {
 border-left: 4px solid #9c27b0;
  }

  .map-card.hideout {
    border-left: 4px solid #4caf50;
  }

  .map-card.secret-realm {
  border-left: 4px solid #ff9800;
  }

  .map-card.boss {
    border-left: 4px solid #f44336;
  }

.map-card.unknown {
    border-left: 4px solid #757575;
  }

  /* 對話框 */
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
  }

  .modal-content {
    background: #1a1a2e;
    border-radius: 12px;
    width: 90%;
    max-width: 500px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
  }

  .modal-header {
    display: flex;
  justify-content: space-between;
    align-items: center;
    padding: 20px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  }

    .modal-header h3 {
      color: white;
      margin: 0;
    }

  .btn-close {
    background: none;
    border: none;
color: rgba(255, 255, 255, 0.6);
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0;
    width: 30px;
    height: 30px;
 display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 4px;
    transition: all 0.3s;
  }

    .btn-close:hover {
      background: rgba(255, 255, 255, 0.1);
      color: white;
    }

  .modal-body {
    padding: 20px;
  }

  .form-group {
 margin-bottom: 20px;
  }

    .form-group label {
      display: block;
      color: rgba(255, 255, 255, 0.8);
      margin-bottom: 8px;
      font-weight: 500;
    }

  .form-input,
  .form-textarea,
  .form-select {
    width: 100%;
    padding: 10px 15px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.2);
    border-radius: 6px;
    color: white;
    font-size: 1rem;
    transition: all 0.3s;
    font-family: inherit;
  }

  .form-textarea {
  resize: vertical;
    min-height: 100px;
  }

    .form-select option {
      background: #1a1a2e;
      color: white;
      padding: 10px;
    }

    .form-input:focus,
    .form-textarea:focus,
    .form-select:focus {
      outline: none;
      border-color: #667eea;
      background: rgba(255, 255, 255, 0.08);
    }

    .form-input:disabled {
      opacity: 0.5;
  cursor: not-allowed;
    }

  .form-hint {
    margin-top: 5px;
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.5);
    font-style: italic;
  }

  .modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    padding: 20px;
    border-top: 1px solid rgba(255, 255, 255, 0.1);
  }

  .btn-cancel,
  .btn-save {
    padding: 10px 20px;
    border: none;
    border-radius: 6px;
    font-size: 1rem;
    cursor: pointer;
    transition: all 0.3s;
  }

  .btn-cancel {
    background: rgba(255, 255, 255, 0.1);
    color: white;
  }

    .btn-cancel:hover {
      background: rgba(255, 255, 255, 0.15);
    }

  .btn-save {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
  }

    .btn-save:hover {
 transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }
</style>
