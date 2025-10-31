<template>
  <div class="pickup-statistics-settings">
    <div class="settings-header">
      <h2>拾取統計設定管理</h2>
      <button @click="showAddDialog = true" class="btn-add">
        <span>➕ 新增統計項目</span>
      </button>
    </div>

    <!-- ✅ 通知訊息 - 改為浮動式 -->
    <Transition name="notification-slide">
      <div v-if="notification.show"
           :class="['notification-float', notification.type]">
        {{ notification.message }}
      </div>
    </Transition>

    <!-- ✅ 載入中 - 改為全螢幕 Overlay -->
    <Transition name="fade">
      <div v-if="loading" class="loading-overlay">
        <div class="loading-spinner">
     <div class="spinner"></div>
          <p>載入中...</p>
        </div>
      </div>
    </Transition>

    <!-- 統計設定列表 -->
    <CollapsibleList :sections="formattedSections"
                     :key="'collapsible-list'">
      <!-- 項目卡片 slot -->
      <template #item="{ items }">
        <div v-for="item in items"
             :key="item.itemId"
             :class="['statistics-card', { disabled: !item.enabled }]" @click="toggleItemEnabled(item)">
          <div class="card-header">
            <div class="item-info">
              <!-- ✅ 星星圖示 - 使用 computed 方法計算路徑 -->
              <img :src="getStarIcon(item)"
                   alt="star"
                   class="item-star-icon"
                   @click.stop="updateItemLike(item)"
                   :style="{ opacity: item.enabled ? 1 : 0.3, cursor: 'pointer' }" />
              <div class="item-name">{{ item.itemName }}</div>
            </div>
            <div class="item-status-icon">
              {{ item.enabled ? '✓' : '✗' }}
            </div>
          </div>
          <div class="card-footer">
            <span class="item-id">ID: {{ item.itemId }}</span>
            <div class="card-actions" @click.stop>
              <button @click="editItem(item)" class="btn-icon" title="編輯">
                ✏️
              </button>
              <button @click="deleteItem(item)" class="btn-icon" title="刪除">
                🗑️
              </button>
            </div>
          </div>
        </div>
      </template>
    </CollapsibleList>

    <!-- 新增/編輯對話框 -->
    <div v-if="showAddDialog" class="modal-overlay" @click.self="closeDialog">
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ isEditing ? '編輯統計項目' : '新增統計項目' }}</h3>
          <button @click="closeDialog" class="btn-close">✕</button>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label>物品 ID *</label>
            <input v-model.number="editingItem.itemId"
                   type="number"
                   placeholder="例如: 1001"
                   :disabled="isEditing"
                   class="form-input" />
          </div>

          <div class="form-group">
            <label>物品名稱 *</label>
            <input v-model="editingItem.itemName"
                   type="text"
                   placeholder="例如: 神威輝石"
                   class="form-input" />
          </div>

          <div class="form-group">
            <label>頁面類型 *</label>
            <select v-model.number="editingItem.pageId" @change="onPageIdChange" class="form-select">
              <option v-for="type in pageIdTypes"
                      :key="type.value"
                      :value="type.value">
                {{ type.name }}
              </option>
            </select>
            <div v-if="pageIdTypes.length === 0" class="form-hint">
              載入頁面類型中...
            </div>
          </div>

          <div class="form-group">
            <label>物品類型 *</label>
            <select v-model="editingItem.itemType" class="form-select">
              <option v-for="type in filteredItemTypes"
                      :key="type.value"
                      :value="type.value">
                {{ type.name }}
              </option>
            </select>
            <div v-if="filteredItemTypes.length === 0" class="form-hint">
              請先選擇頁面類型
            </div>
          </div>

          <div class="form-group">
            <label class="checkbox-label">
              <input v-model="editingItem.enabled"
                     type="checkbox"
                     class="form-checkbox" />
              <span>啟用統計</span>
            </label>
          </div>
        </div>

        <div class="modal-footer">
          <button @click="closeDialog" class="btn-cancel">取消</button>
          <button @click="saveItem" class="btn-save">儲存</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { apiCall } from '../utils/api'
  import CollapsibleList from './CollapsibleList.vue'

  const loading = ref(false)
  const statisticsConfigs = ref({})
  const pageIdTypes = ref([])
  const itemTypes = ref([])
  const pageIdItemTypeMapping = ref({})
  const showAddDialog = ref(false)
  const isEditing = ref(false)
  const notification = ref({ show: false, type: 'success', message: '' })

  const editingItem = ref({
    itemId: 0,
    itemName: '',
    pageId: 100,
    itemType: '',
    enabled: true
  })

  // 格式化資料給 CollapsibleList
  const formattedSections = computed(() => {
    return pageIdTypes.value.map(pageType => {
      const itemTypeGroups = statisticsConfigs.value[pageType.value] || {}
      const totalCount = Object.values(itemTypeGroups).reduce((total, items) => total + items.length, 0)

      // 建立子分類
      const subcategories = Object.entries(itemTypeGroups).map(([itemType, items]) => ({
        key: itemType,
        name: getItemTypeName(itemType),
        items: items
      }))

      return {
        key: pageType.value,
        name: pageType.name,
        totalCount: totalCount,
        subcategories: subcategories
      }
    })
  })

  // 獲取 ItemType 的顯示名稱
  const getItemTypeName = (itemType) => {
    const type = itemTypes.value.find(t => t.value === itemType)
    return type ? type.name : itemType
  }

  // ✅ 新增：計算星星圖示路徑
  const getStarIcon = (item) => {
    const like = item.like ?? 0
    // 確保 like 值在 0-6 範圍內
    const safelike = Math.max(0, Math.min(6, like))
    return `/assets/icons/star-${safelike}.svg`
  }

  // 根據選擇的 PageId 過濾可用的 ItemType
  const filteredItemTypes = computed(() => {
    const pageId = editingItem.value.pageId
    if (!pageId || !pageIdItemTypeMapping.value[pageId]) {
      return itemTypes.value
    }

    const allowedTypes = pageIdItemTypeMapping.value[pageId]
    return itemTypes.value.filter(t => allowedTypes.includes(t.value))
  })

  // 監聽 PageId 變化，自動調整 ItemType
  const onPageIdChange = () => {
    const currentItemType = editingItem.value.itemType
    const allowedTypes = pageIdItemTypeMapping.value[editingItem.value.pageId] || []

    if (currentItemType && !allowedTypes.includes(currentItemType)) {
      if (filteredItemTypes.value.length > 0) {
        editingItem.value.itemType = filteredItemTypes.value[0].value
      } else {
        editingItem.value.itemType = ''
      }
    }
  }

  // 切換項目啟用狀態
  const toggleItemEnabled = async (item) => {
    const newEnabled = !item.enabled

    try {
      const result = await apiCall(
 'SavePickupStatisticsItem',
     item.itemId,
        item.itemName,
 item.pageId,
        newEnabled,
    item.itemType
      )

      if (result.success) {
        item.enabled = newEnabled
        // ✅ 移除通知，直接更新狀態
      } else {
 // ❌ 只在失敗時顯示錯誤
     showNotification('error', result.message)
      }
    } catch (err) {
      showNotification('error', '更新狀態失敗: ' + err.message)
    }
  }

  // ✅ 新增：更新物品的 Like 值
  const updateItemLike = async (item) => {
    try {
  const result = await apiCall('UpdateItemLike', item.itemId)

      if (result.success) {
  // ✅ 直接更新物件的屬性，避免替換整個物件
        // 使用 Vue 3 的響應式更新，不會觸發整個列表重新渲染
    item.like = result.like

        // ✅ 如果後端也返回 enabled 狀態，同時更新
   if (result.hasOwnProperty('enabled')) {
   item.enabled = result.enabled
        }

        // ✅ 移除通知，直接更新星星
 } else {
        // ❌ 只在失敗時顯示錯誤
    showNotification('error', result.message)
      }
    } catch (err) {
      showNotification('error', '更新星星失敗: ' + err.message)
    }
  }

  // 載入頁面類型
  const loadPageIdTypes = async () => {
    try {
      const data = await apiCall('GetPageIdTypes')
      pageIdTypes.value = data

      if (pageIdTypes.value.length > 0 && editingItem.value.pageId === 0) {
        editingItem.value.pageId = pageIdTypes.value[0].value
      }
    } catch (err) {
      console.error('載入頁面類型失敗:', err)
      showNotification('error', '載入頁面類型失敗: ' + err.message)
    }
  }

  // 載入物品類型
  const loadItemTypes = async () => {
    try {
      const data = await apiCall('GetItemTypes')
      itemTypes.value = data

      if (itemTypes.value.length > 0 && !editingItem.value.itemType) {
        editingItem.value.itemType = itemTypes.value[0].value
      }
    } catch (err) {
      console.error('載入物品類型失敗:', err)
      showNotification('error', '載入物品類型失敗: ' + err.message)
    }
  }

  // 載入 PageId 和 ItemType 對應關係
  const loadPageIdItemTypeMapping = async () => {
    try {
      const data = await apiCall('GetPageIdItemTypeMapping')
      pageIdItemTypeMapping.value = data
    } catch (err) {
      console.error('載入對應關係失敗:', err)
      showNotification('error', '載入對應關係失敗: ' + err.message)
    }
  }

  // 載入統計設定
  const loadStatisticsConfigs = async () => {
    loading.value = true
    try {
      const data = await apiCall('GetPickupStatisticsConfigs')
      statisticsConfigs.value = data
    } catch (err) {
      showNotification('error', '載入統計設定失敗: ' + err.message)
    } finally {
      loading.value = false
    }
  }

  // 編輯項目
  const editItem = (item) => {
    editingItem.value = { ...item }
    isEditing.value = true
    showAddDialog.value = true
  }

  // 儲存項目
  const saveItem = async () => {
    if (!editingItem.value.itemId || !editingItem.value.itemName || !editingItem.value.itemType) {
      showNotification('error', '請填寫所有必填欄位')
      return
    }

    try {
      const result = await apiCall(
        'SavePickupStatisticsItem',
        editingItem.value.itemId,
        editingItem.value.itemName,
        editingItem.value.pageId,
        editingItem.value.enabled,
        editingItem.value.itemType
      )

      if (result.success) {
        showNotification('success', result.message)
        closeDialog()
        await loadStatisticsConfigs()
      } else {
        showNotification('error', result.message)
      }
    } catch (err) {
      showNotification('error', '儲存失敗: ' + err.message)
    }
  }

  // 刪除項目
  const deleteItem = async (item) => {
    if (!confirm(`確定要刪除統計項目「${item.itemName}」嗎？`)) {
      return
    }

    try {
      const result = await apiCall('DeletePickupStatisticsItem', item.pageId, item.itemId)

      if (result.success) {
        showNotification('success', result.message)
        await loadStatisticsConfigs()
      } else {
        showNotification('error', result.message)
      }
    } catch (err) {
      showNotification('error', '刪除失敗: ' + err.message)
    }
  }

  // 閉對話框
  const closeDialog = () => {
    showAddDialog.value = false
    isEditing.value = false
    editingItem.value = {
      itemId: 0,
      itemName: '',
      pageId: pageIdTypes.value.length > 0 ? pageIdTypes.value[0].value : 100,
      itemType: itemTypes.value.length > 0 ? itemTypes.value[0].value : '',
      enabled: true
    }
  }

  // 顯示通知
  const showNotification = (type, message) => {
    notification.value = { show: true, type, message }
    setTimeout(() => {
      notification.value.show = false
    }, 3000)
  }

  // 監聽後端的設定更新通知
  if (typeof window !== 'undefined') {
    window.addEventListener('message', (event) => {
      try {
        const message = typeof event.data === 'string' ? JSON.parse(event.data) : event.data

        if (message && message.type === 'pickupStatisticsConfigUpdated') {
          const { success, message: msg } = message.data
          showNotification(success ? 'success' : 'error', msg)
          if (success) {
            loadStatisticsConfigs()
          }
        }
      } catch (err) {
        console.error('Failed to parse message:', err)
      }
    })
  }

  onMounted(async () => {
    await loadPageIdTypes()
    await loadItemTypes()
    await loadPageIdItemTypeMapping()
    await loadStatisticsConfigs()
  })
</script>

<style scoped>
  .pickup-statistics-settings {
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

  /* 通知訊息 */
  /* ✅ 浮動通知 - 固定在頂部中央 */
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

  /* ✅ 通知動畫 - 從上方滑入 */
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

  /* 載入中 */
  /* ✅ 載入中 - 全螢幕 Overlay */
  .loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    backdrop-filter: blur(5px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9998;
  }

  .loading-spinner {
    text-align: center;
  }

  .spinner {
    width: 50px;
 height: 50px;
    margin: 0 auto 20px;
    border: 4px solid rgba(255, 255, 255, 0.3);
    border-top-color: #667eea;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }

  .loading-spinner p {
    color: white;
 font-size: 1.2rem;
    font-weight: 500;
  }

  /* ✅ Fade 動畫 */
  .fade-enter-active,
  .fade-leave-active {
    transition: opacity 0.3s ease;
  }

  .fade-enter-from,
  .fade-leave-to {
    opacity: 0;
  }

  /* 統計卡片 */
  .statistics-card {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border: 2px solid rgba(255, 255, 255, 0.1);
    border-radius: 12px;
    padding: 20px;
    cursor: pointer;
    transition: all 0.3s;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  
    .statistics-card:hover {
      border-color: rgba(255, 255, 255, 0.3);
      transform: translateY(-5px);
      box-shadow: 0 8px 20px rgba(0, 0, 0, 0.3);
    }

    .statistics-card.disabled {
      opacity: 0.5;
      border-color: rgba(244, 67, 54, 0.3);
    }

      .statistics-card.disabled:hover {
        border-color: rgba(244, 67, 54, 0.5);
      }

  /* 卡片頭部 */
  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 10px;
    padding-bottom: 12px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  }

  /* ✅ 新增：項目資訊容器 */
  .item-info {
    display: flex;
    align-items: center;
    gap: 10px;
    flex: 1;
  }

  /* ✅ 新增：星星圖示樣式 */
  .item-star-icon {
    width: 24px;
    height: 24px;
    flex-shrink: 0;
    filter: drop-shadow(0 0 4px rgba(255, 215, 0, 0.5));
    transition: all 0.3s;
    cursor: pointer;
  }

  .statistics-card:not(.disabled) .item-star-icon {
    color: #ffd700;
  }

  .statistics-card.disabled .item-star-icon {
    color: rgba(255, 255, 255, 0.3);
  }

  .item-star-icon:hover {
    transform: scale(1.2) rotate(20deg);
    filter: drop-shadow(0 0 8px rgba(255, 215, 0, 0.8));
  }

  .item-star-icon:active {
    transform: scale(1.1) rotate(10deg);
  }

  .item-name {
    color: white;
    font-size: 1.1rem;
    font-weight: 600;
    flex: 1;
    word-break: break-word;
  }

  .item-status-icon {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1rem;
    font-weight: bold;
    flex-shrink: 0;
    transition: all 0.3s;
  }

  .statistics-card:not(.disabled) .item-status-icon {
    background: rgba(76, 175, 80, 0.2);
    color: #4caf50;
  }

  .statistics-card.disabled .item-status-icon {
    background: rgba(244, 67, 54, 0.2);
    color: #f44336;
  }

  /* 卡片底部 */
  .card-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 10px;
  }

  .item-id {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.6);
  }

  .card-actions {
    display: flex;
    gap: 8px;
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

    .form-select option {
      background: #1a1a2e;
      color: white;
      padding: 10px;
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

  .form-hint {
    margin-top: 5px;
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.5);
    font-style: italic;
  }

  .checkbox-label {
    display: flex;
    align-items: center;
    gap: 10px;
    cursor: pointer;
    user-select: none;
  }

  .form-checkbox {
    width: 18px;
    height: 18px;
    cursor: pointer;
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
