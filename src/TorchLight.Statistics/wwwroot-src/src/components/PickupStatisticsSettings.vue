<template>
    <div class="pickup-statistics-settings">
        <div class="settings-header">
            <h2>拾取統計設定管理</h2>
            <button @click="showAddDialog = true" class="btn-add">
                <span>➕ 新增統計項目</span>
            </button>
        </div>

        <!-- 通知訊息 -->
        <div v-if="notification.show" :class="['notification', notification.type]">
            {{ notification.message }}
        </div>

        <!-- 載入中 -->
        <div v-if="loading" class="loading">載入中...</div>

        <!-- 統計設定列表 -->
        <div v-else class="statistics-container">
            <!-- 動態渲染 PageId 類型區塊 -->
            <div v-for="pageType in pageIdTypes"
                 :key="pageType.value"
                 class="statistics-section">
                <!-- 大分類標題與折疊按鈕 -->
                <div class="section-header" @click="toggleCollapse(pageType.value)">
                    <h3>
                        <span class="collapse-icon">{{ isCollapsed(pageType.value) ? '▶' : '▼' }}</span>
                        {{ pageType.name }}
                        <span class="count-badge">({{ getTotalItemsByPageId(pageType.value) }})</span>
                    </h3>
                </div>

                <!-- 小分類（ItemType）區塊 -->
                <div v-show="!isCollapsed(pageType.value)" class="subcategories">
                    <div v-for="(items, itemType) in getItemTypesByPageId(pageType.value)"
                         :key="`${pageType.value}-${itemType}`"
                         class="subcategory-section">
                        <!-- 小分類標題 -->
                        <div class="subcategory-header" @click="toggleSubcategoryCollapse(pageType.value, itemType)">
                            <h4>
                                <span class="collapse-icon">{{ isSubcategoryCollapsed(pageType.value, itemType) ? '▶' : '▼' }}</span>
                                {{ getItemTypeName(itemType) }}
                                <span class="count-badge">({{ items.length }})</span>
                            </h4>
                        </div>

                        <!-- 統計項目網格（可折疊） -->
                        <div v-show="!isSubcategoryCollapsed(pageType.value, itemType)" class="statistics-grid">
                            <div v-for="item in items"
                                 :key="item.itemId"
                                 :class="['statistics-card', { disabled: !item.enabled }]"
                                 @click="toggleItemEnabled(item)">
                                <div class="card-header">
                                    <div class="item-name">{{ item.itemName }}</div>
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
                        </div>
                    </div>

                    <!-- 空狀態提示 -->
                    <div v-if="getTotalItemsByPageId(pageType.value) === 0" class="empty-message">
                        尚未設定{{ pageType.name.replace(/^.+?\s/, '') }}統計項目
                    </div>
                </div>
            </div>
        </div>

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

    const loading = ref(false)
    const statisticsConfigs = ref({})
    const pageIdTypes = ref([])
    const itemTypes = ref([])
    const pageIdItemTypeMapping = ref({}) // 新增：PageId 和 ItemType 對應關係
    const showAddDialog = ref(false)
    const isEditing = ref(false)
    const notification = ref({ show: false, type: 'success', message: '' })

    const collapsedSections = ref({})
    const collapsedSubcategories = ref({})

    const editingItem = ref({
        itemId: 0,
        itemName: '',
        pageId: 100,
        itemType: '',
        enabled: true
    })

    // 切換大分類折疊狀態
    const toggleCollapse = (pageId) => {
        collapsedSections.value[pageId] = !collapsedSections.value[pageId]
    }

    // 檢查大分類是否已折疊（預設為 true，即折疊）
    const isCollapsed = (pageId) => {
        // 如果未設定，預設為 true（折疊）
        return collapsedSections.value[pageId] !== false
    }

    // 切換小分類折疊狀態
    const toggleSubcategoryCollapse = (pageId, itemType) => {
        const key = `${pageId}-${itemType}`
        collapsedSubcategories.value[key] = !collapsedSubcategories.value[key]
    }

    // 檢查小分類是否已折疊（預設為 true，即折疊）
    const isSubcategoryCollapsed = (pageId, itemType) => {
        const key = `${pageId}-${itemType}`
        // 如果未設定，預設為 true（折疊）
        return collapsedSubcategories.value[key] !== false
    }

    // 根據 PageId 獲取所有 ItemType 分組
    const getItemTypesByPageId = (pageId) => {
        return statisticsConfigs.value[pageId] || {}
    }

    // 獲取 PageId 下的總項目數
    const getTotalItemsByPageId = (pageId) => {
        const itemTypeGroups = statisticsConfigs.value[pageId]
        if (!itemTypeGroups) return 0

        return Object.values(itemTypeGroups).reduce((total, items) => total + items.length, 0)
    }

    // 獲取 ItemType 的顯示名稱
    const getItemTypeName = (itemType) => {
        const type = itemTypes.value.find(t => t.value === itemType)
        return type ? type.name : itemType
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

        // 如果當前選擇的 ItemType 不在允許的列表中，重置為第一個可用選項
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
                showNotification('success', newEnabled ? '已啟用' : '已停用')
            } else {
                showNotification('error', result.message)
            }
        } catch (err) {
            showNotification('error', '更新狀態失敗: ' + err.message)
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

    // 關閉對話框
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
        await loadPageIdItemTypeMapping() // 新增：載入對應關係
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

    /* 載入中 */
    .loading {
        text-align: center;
        padding: 60px;
        color: rgba(255, 255, 255, 0.7);
    }

    /* 統計設定區塊 */
    .statistics-section {
        margin-bottom: 30px;
    }

    /* 大分類標題 */
    .section-header {
        display: flex;
        align-items: center;
        cursor: pointer;
        user-select: none;
        padding: 15px 20px;
        background: rgba(255, 255, 255, 0.05);
        border-radius: 10px;
        margin-bottom: 15px;
        transition: all 0.3s;
        border-left: 4px solid rgba(102, 126, 234, 0.5);
    }

        .section-header:hover {
            background: rgba(255, 255, 255, 0.08);
            border-left-color: #667eea;
        }

        .section-header h3 {
            margin: 0;
            display: flex;
            align-items: center;
            gap: 12px;
            color: white;
            font-size: 1.4rem;
            font-weight: 600;
        }

    /* 小分類區塊容器 */
    .subcategories {
        padding-left: 20px;
        animation: slideDown 0.3s ease-out;
    }

    /* 小分類標題 */
    .subcategory-section {
        margin-bottom: 20px;
    }

    .subcategory-header {
        display: flex;
        align-items: center;
        cursor: pointer;
        user-select: none;
        padding: 10px 15px;
        background: rgba(255, 255, 255, 0.03);
        border-radius: 8px;
        margin-bottom: 12px;
        transition: all 0.3s;
        border-left: 3px solid rgba(255, 255, 255, 0.2);
    }

        .subcategory-header:hover {
            background: rgba(255, 255, 255, 0.06);
            border-left-color: rgba(102, 126, 234, 0.8);
        }

        .subcategory-header h4 {
            margin: 0;
            display: flex;
            align-items: center;
            gap: 10px;
            color: rgba(255, 255, 255, 0.9);
            font-size: 1.1rem;
            font-weight: 500;
        }

    .collapse-icon {
        font-size: 0.9rem;
        transition: transform 0.3s;
        display: inline-block;
        width: 20px;
        color: rgba(255, 255, 255, 0.6);
    }

    .count-badge {
        font-size: 0.85rem;
        color: rgba(255, 255, 255, 0.5);
        font-weight: normal;
    }

    /* 網格佈局 */
    .statistics-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 15px;
        padding-left: 20px;
        animation: slideDown 0.3s ease-out;
    }

    @keyframes slideDown {
        from {
            opacity: 0;
            transform: translateY(-10px);
        }

        to {
            opacity: 1;
            transform: translateY(0);
        }
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

    .empty-message {
        text-align: center;
        padding: 30px;
        color: rgba(255, 255, 255, 0.5);
        background: rgba(255, 255, 255, 0.05);
        border-radius: 8px;
        margin-left: 20px;
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

    /* 響應式設計 */
    @media (max-width: 1200px) {
        .statistics-grid {
            grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        }
    }

    @media (max-width: 768px) {
        .statistics-grid {
            grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
            padding-left: 10px;
        }

        .subcategories {
            padding-left: 10px;
        }
    }

    @media (max-width: 480px) {
        .statistics-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
