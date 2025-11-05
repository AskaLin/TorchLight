<template>
  <div v-if="show" class="modal-overlay" @click.self="closeDialog">
    <div class="modal-content">
      <div class="modal-header">
        <h3>{{ isMapEdit ? '編輯地圖' : '編輯物品' }}</h3>
        <button @click="closeDialog" class="btn-close">✕</button>
      </div>

      <div class="modal-body">
        <!-- 地圖編輯 -->
        <template v-if="isMapEdit">
          <div class="form-group">
            <label>地圖名稱 *</label>
            <input v-model="formData.mapName"
                   type="text"
                   placeholder="例如: 雜蕪街區"
                   class="form-input" />
            <div class="form-hint">輸入地圖名稱（不包含等級前綴）</div>
          </div>

          <div class="form-group">
            <label>地圖 ID *</label>
            <input v-model="formData.mapId"
                   type="text"
                   :disabled="true"
                   class="form-input disabled-input" />
            <div class="form-hint">地圖 ID 由系統自動設定</div>
          </div>

          <div class="form-group">
            <label>地圖類型 *</label>
            <select v-model="formData.mapType" class="form-select">
              <option v-for="type in mapTypes"
                      :key="type.value"
                      :value="type.value">
                {{ type.name }}
              </option>
            </select>
          </div>
        </template>

        <!-- 物品編輯 -->
        <template v-else>
          <div class="form-group">
            <label>物品名稱 *</label>
            <input v-model="formData.itemName"
                   type="text"
                   placeholder="例如: 混沌石"
                   class="form-input" />
            <div class="form-hint">輸入物品的正確名稱</div>
          </div>

          <div class="form-group">
            <label>物品 ID *</label>
            <input v-model="formData.itemId"
                   type="text"
                   :disabled="true"
                   class="form-input disabled-input" />
            <div class="form-hint">物品 ID 由系統自動設定</div>
          </div>

          <div class="form-group">
            <label>PageId *</label>
            <select v-model="formData.pageId" class="form-select" disabled>
              <option v-for="type in pageIdTypes"
                      :key="type.value"
                      :value="type.value">
                {{ type.name }}
              </option>
            </select>
            <div class="form-hint">PageId 由系統自動判斷</div>
          </div>

          <div class="form-group">
            <label>物品類型 *</label>
            <select v-model="formData.itemType" class="form-select">
              <option v-for="type in filteredItemTypes"
                      :key="type.value"
                      :value="type.value">
                {{ type.name }}
              </option>
            </select>
            <div class="form-hint">選擇物品所屬的類型</div>
          </div>

          <div class="form-group">
            <label class="checkbox-label">
              <input type="checkbox" v-model="formData.enabled" />
              啟用統計
            </label>
            <div class="form-hint">是否在統計中顯示此物品</div>
          </div>

          <div class="form-group">
            <label class="checkbox-label">
              <input type="checkbox" v-model="formData.watch" />
              監控物品
            </label>
            <div class="form-hint">在浮動窗口中顯示此物品的數量</div>
          </div>
        </template>
      </div>

      <div class="modal-footer">
        <button @click="closeDialog" class="btn-cancel">取消</button>
        <button @click="saveChanges" class="btn-save" :disabled="!isFormValid">
          儲存
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { apiCall } from '../utils/api'
import { useNotification } from '../composables/useNotification'

const props = defineProps({
  show: Boolean,
  editType: String, // 'map' or 'item'
  editData: Object
})

const emit = defineEmits(['close', 'saved'])

const { showNotification } = useNotification()

// 表單資料
const formData = ref({
  // 地圖編輯
  mapName: '',
  mapId: '',
  mapType: 'Netherrealm',
  
  // 物品編輯
  itemName: '',
  itemId: '',
  pageId: 102,
  itemType: 'Currency',
  enabled: true,
  watch: false
})

// 地圖類型
const mapTypes = ref([])

// PageId 類型
const pageIdTypes = ref([])

// 物品類型
const itemTypes = ref([])

// PageId 和 ItemType 對應關係
const pageIdItemTypeMapping = ref({})

// 🆕 根據 PageId 過濾物品類型
const filteredItemTypes = computed(() => {
  if (!formData.value.pageId || !pageIdItemTypeMapping.value[formData.value.pageId]) {
    return itemTypes.value
  }
  
  const allowedTypes = pageIdItemTypeMapping.value[formData.value.pageId]
  return itemTypes.value.filter(type => allowedTypes.includes(type.value))
})

// 是否為地圖編輯
const isMapEdit = computed(() => props.editType === 'map')

// 表單驗證
const isFormValid = computed(() => {
  if (isMapEdit.value) {
    return formData.value.mapName && formData.value.mapId && formData.value.mapType
  } else {
    return formData.value.itemName && formData.value.itemId && formData.value.itemType
  }
})

// 監聽 props 變化
watch(() => props.show, async (newVal) => {
  if (newVal) {
    await loadOptions()
    loadFormData()
  }
})

// 載入選項
async function loadOptions() {
  try {
    if (isMapEdit.value) {
      // 載入地圖類型
      const types = await apiCall('GetMapTypes')
      mapTypes.value = types
    } else {
      // 載入 PageId 類型
      const pageTypes = await apiCall('GetPageIdTypes')
      pageIdTypes.value = pageTypes
      
      // 載入物品類型
      const types = await apiCall('GetItemTypes')
      itemTypes.value = types
      
      // 載入 PageId 和 ItemType 對應關係
      const mapping = await apiCall('GetPageIdItemTypeMapping')
      pageIdItemTypeMapping.value = mapping
    }
  } catch (error) {
    console.error('載入選項失敗:', error)
  }
}

// 載入表單資料
function loadFormData() {
  if (!props.editData) return

  console.log(props.editData)
  if (isMapEdit.value) {
    formData.value.mapName = props.editData.mapName || ''
    formData.value.mapId = props.editData.mapId || ''
    formData.value.mapType = props.editData.mapType || 'Netherrealm'
  } else {
    formData.value.itemName = props.editData.itemName || ''
    formData.value.itemId = props.editData.itemId || ''
    formData.value.pageId = props.editData.pageId || 102
    formData.value.itemType = props.editData.itemType || 'Currency'
    formData.value.enabled = props.editData.enabled !== false
    formData.value.watch = props.editData.watch || false
  }
}

// 儲存變更
async function saveChanges() {
  try {
    let result
    
    if (isMapEdit.value) {
      // 儲存地圖
      result = await apiCall(
        'SaveMapConfig',
        formData.value.mapName,
        JSON.stringify([parseInt(formData.value.mapId)]),
        formData.value.mapType
      )
    } else {
      // 儲存物品
      result = await apiCall(
        'SavePickupStatisticsItem',
        parseInt(formData.value.itemId),
        formData.value.itemName,
        formData.value.pageId,
        formData.value.enabled,
        formData.value.itemType,
        formData.value.watch
      )
    }
    
    if (result.success) {
      showNotification('success', '儲存成功')
      emit('saved')
      closeDialog()
    } else {
      showNotification('error', result.message || '儲存失敗')
    }
  } catch (error) {
    console.error('儲存失敗:', error)
    showNotification('error', '儲存時發生錯誤')
  }
}

// 關閉對話框
function closeDialog() {
  emit('close')
}
</script>

<style scoped>
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
  z-index: 2000;
}

.modal-content {
  background: #1a1a2e;
  border-radius: 12px;
  width: 90%;
  max-width: 500px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
  max-height: 90vh;
  overflow-y: auto;
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

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}

.checkbox-label input[type="checkbox"] {
  width: 18px;
  height: 18px;
  cursor: pointer;
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
  font-family: inherit;
}

.form-input:focus,
.form-select:focus {
  outline: none;
  border-color: #667eea;
  background: rgba(255, 255, 255, 0.08);
}

.disabled-input {
  opacity: 0.5;
  cursor: not-allowed;
}

.form-select option {
  background: #1a1a2e;
  color: white;
  padding: 10px;
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

.btn-save:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn-save:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
