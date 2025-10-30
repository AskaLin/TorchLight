<template>
  <div class="map-settings">
    <div class="settings-header">
      <h2>地圖設定管理</h2>
      <button @click="showAddDialog = true" class="btn-add">
     <span>➕ 新增地圖</span>
      </button>
    </div>

    <!-- 通知訊息 -->
  <div v-if="notification.show" :class="['notification', notification.type]">
      {{ notification.message }}
    </div>

    <!-- 載入中 -->
    <div v-if="loading" class="loading">載入中...</div>

    <!-- 地圖列表 -->
    <div v-else class="maps-container">
      <!-- 藏身處地圖 -->
      <div class="map-section">
        <h3>🏠 藏身處地圖</h3>
        <div class="maps-grid">
          <div
            v-for="map in hideoutMaps"
        :key="map.mapId"
      class="map-card hideout"
        >
            <div class="map-info">
   <div class="map-name">{{ map.mapName }}</div>
      <div class="map-id">ID: {{ map.mapId }}</div>
     </div>
            <div class="map-actions">
       <button @click="editMap(map)" class="btn-edit" title="編輯">
        ✏️
              </button>
    <button @click="deleteMap(map)" class="btn-delete" title="刪除">
 🗑️
  </button>
     </div>
          </div>
        </div>
        <div v-if="hideoutMaps.length === 0" class="empty-message">
        尚未設定藏身處地圖
        </div>
      </div>

      <!-- 異界地圖 -->
      <div class="map-section">
   <h3>🌌 異界地圖</h3>
     <div class="maps-grid">
          <div
        v-for="map in netherrealmMaps"
   :key="map.mapId"
            class="map-card netherrealm"
  >
            <div class="map-info">
      <div class="map-name">{{ map.mapName }}</div>
              <div class="map-id">ID: {{ map.mapId }}</div>
    </div>
            <div class="map-actions">
         <button @click="editMap(map)" class="btn-edit" title="編輯">
           ✏️
  </button>
              <button @click="deleteMap(map)" class="btn-delete" title="刪除">
    🗑️
    </button>
  </div>
   </div>
 </div>
        <div v-if="netherrealmMaps.length === 0" class="empty-message">
        尚未設定異界地圖
        </div>
      </div>

      <!-- 未知地圖 -->
  <div v-if="unknownMaps.length > 0" class="map-section">
        <h3>❓ 未分類地圖</h3>
   <div class="maps-grid">
   <div
        v-for="map in unknownMaps"
:key="map.mapId"
        class="map-card unknown"
          >
<div class="map-info">
    <div class="map-name">{{ map.mapName }}</div>
    <div class="map-id">ID: {{ map.mapId }}</div>
  </div>
  <div class="map-actions">
       <button @click="editMap(map)" class="btn-edit" title="編輯">
  ✏️
     </button>
   <button @click="deleteMap(map)" class="btn-delete" title="刪除">
      🗑️
     </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 新增/編輯對話框 -->
    <div v-if="showAddDialog" class="modal-overlay" @click.self="closeDialog">
      <div class="modal-content">
        <div class="modal-header">
      <h3>{{ isEditing ? '編輯地圖' : '新增地圖' }}</h3>
      <button @click="closeDialog" class="btn-close">✕</button>
        </div>

        <div class="modal-body">
     <div class="form-group">
     <label>地圖 ID *</label>
          <input
         v-model="editingMap.mapId"
              type="text"
              placeholder="例如: GeBuLinCunLuo01"
    :disabled="isEditing"
class="form-input"
       />
    </div>

          <div class="form-group">
            <label>地圖名稱 *</label>
     <input
  v-model="editingMap.mapName"
  type="text"
      placeholder="例如: 隔壁林村落01"
  class="form-input"
  />
          </div>

          <div class="form-group">
        <label>地圖類型 *</label>
    <select v-model="editingMap.mapType" class="form-select">
        <option value="Hideout">藏身處</option>
              <option value="Netherrealm">異界地圖</option>
   </select>
          </div>
        </div>

      <div class="modal-footer">
     <button @click="closeDialog" class="btn-cancel">取消</button>
   <button @click="saveMap" class="btn-save">儲存</button>
   </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { apiCall } from '../utils/api'

const loading = ref(false)
const maps = ref([])
const showAddDialog = ref(false)
const isEditing = ref(false)
const notification = ref({ show: false, type: 'success', message: '' })

const editingMap = ref({
  mapId: '',
  mapName: '',
  mapType: 'Netherrealm'
})

// 分類地圖
const hideoutMaps = computed(() =>
  maps.value.filter(m => m.mapType === 'Hideout')
)

const netherrealmMaps = computed(() =>
  maps.value.filter(m => m.mapType === 'Netherrealm')
)

const unknownMaps = computed(() =>
  maps.value.filter(m => m.mapType === 'Unknown')
)

// 載入地圖設定
const loadMaps = async () => {
  loading.value = true
  try {
    const data = await apiCall('GetMapConfigs')
    maps.value = data
  } catch (err) {
    showNotification('error', '載入地圖設定失敗: ' + err.message)
  } finally {
    loading.value = false
  }
}

// 編輯地圖
const editMap = (map) => {
  editingMap.value = { ...map }
  isEditing.value = true
  showAddDialog.value = true
}

// 儲存地圖
const saveMap = async () => {
  if (!editingMap.value.mapId || !editingMap.value.mapName) {
    showNotification('error', '請填寫所有必填欄位')
    return
  }

  try {
    const result = await apiCall(
   'SaveMapConfig',
      editingMap.value.mapId,
      editingMap.value.mapName,
      editingMap.value.mapType
    )

    if (result.success) {
      showNotification('success', result.message)
      closeDialog()
      await loadMaps()
    } else {
      showNotification('error', result.message)
    }
  } catch (err) {
    showNotification('error', '儲存失敗: ' + err.message)
  }
}

// 刪除地圖
const deleteMap = async (map) => {
  if (!confirm(`確定要刪除地圖「${map.mapName}」嗎？`)) {
    return
  }

  try {
    const result = await apiCall('DeleteMapConfig', map.mapId)

    if (result.success) {
      showNotification('success', result.message)
      await loadMaps()
    } else {
      showNotification('error', result.message)
    }
  } catch (err) {
    showNotification('error', '刪除失敗: ' + err.message)
  }
}

// 關閉對話框
const closeDialog = () => {
  showAddDialog.value = false
  isEditing.value = false
  editingMap.value = {
    mapId: '',
    mapName: '',
    mapType: 'Netherrealm'
  }
}

// 顯示通知
const showNotification = (type, message) => {
  notification.value = { show: true, type, message }
  setTimeout(() => {
    notification.value.show = false
  }, 5000)
}

// 監聽後端的設定更新通知
if (typeof window !== 'undefined') {
  window.addEventListener('message', (event) => {
    try {
      const message = typeof event.data === 'string' ? JSON.parse(event.data) : event.data

      if (message && message.type === 'mapConfigUpdated') {
        const { success, message: msg } = message.data
        showNotification(success ? 'success' : 'error', msg)
        if (success) {
          loadMaps()
        }
   }
    } catch (err) {
      console.error('Failed to parse message:', err)
    }
  })
}

onMounted(() => {
  loadMaps()
})
</script>

<style scoped>
.map-settings {
  max-width: 1400px;
  margin: 0 auto;
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

/* 載入中 */
.loading {
  text-align: center;
  padding: 60px;
  color: rgba(255, 255, 255, 0.7);
}

/* 地圖區塊 */
.map-section {
  margin-bottom: 40px;
}

.map-section h3 {
  color: white;
  margin: 0 0 15px 0;
  font-size: 1.3rem;
}

.maps-grid {
display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 15px;
}

.map-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  border-radius: 8px;
  transition: all 0.3s;
}

.map-card.hideout {
background: rgba(76, 175, 80, 0.1);
  border: 1px solid rgba(76, 175, 80, 0.3);
}

.map-card.netherrealm {
  background: rgba(156, 39, 176, 0.1);
  border: 1px solid rgba(156, 39, 176, 0.3);
}

.map-card.unknown {
  background: rgba(255, 152, 0, 0.1);
  border: 1px solid rgba(255, 152, 0, 0.3);
}

.map-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.map-info {
  flex: 1;
}

.map-name {
  color: white;
  font-size: 1.1rem;
  font-weight: 600;
  margin-bottom: 5px;
}

.map-id {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.85rem;
}

.map-actions {
  display: flex;
  gap: 10px;
}

.btn-edit,
.btn-delete {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 6px;
  font-size: 1.2rem;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-edit {
  background: rgba(33, 150, 243, 0.2);
}

.btn-edit:hover {
  background: rgba(33, 150, 243, 0.4);
}

.btn-delete {
  background: rgba(244, 67, 54, 0.2);
}

.btn-delete:hover {
  background: rgba(244, 67, 54, 0.4);
}

.empty-message {
text-align: center;
  padding: 30px;
  color: rgba(255, 255, 255, 0.5);
  background: rgba(255, 255, 255, 0.05);
  border-radius: 8px;
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
.form-select {
  width: 100%;
  padding: 10px 15px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 6px;
  color: white;
  font-size: 1rem;
  transition: all 0.3s;
}

.form-input:focus,
.form-select:focus {
  outline: none;
  border-color: #667eea;
  background: rgba(255, 255, 255, 0.08);
}

.form-input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
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
