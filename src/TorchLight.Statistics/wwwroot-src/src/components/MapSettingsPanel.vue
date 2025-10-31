<template>
    <div class="map-settings-panel">
        <!-- 設定標題與新增按鈕 -->
        <div class="settings-header">
            <h2>地圖設定管理</h2>
            <button @click="showAddMapDialog = true" class="btn-add">
                <span>➕ 新增地圖</span>
            </button>
        </div>

        <!-- 通知訊息 -->
        <div v-if="notification.show" :class="['notification', notification.type]">
            {{ notification.message }}
        </div>

        <!-- 地圖列表 -->
        <div class="maps-container">
            <!-- 動態渲染地圖類型區塊 -->
            <div v-for="type in mapTypes" :key="type.value" class="map-section">
                <!-- 標題與折疊按鈕 -->
                <div class="section-header" @click="toggleCollapse(type.value)">
                    <h3>
                        <span class="collapse-icon">{{ isCollapsed(type.value) ? '▶' : '▼' }}</span>
                        {{ type.name }}
                        <span class="count-badge">({{ mapsByType[type.value]?.length || 0 }})</span>
                    </h3>
                </div>

                <!-- 地圖網格（可折疊） -->
                <div v-show="!isCollapsed(type.value)" class="maps-grid">
                    <div v-for="map in mapsByType[type.value]"
                         :key="map.mapId"
                         :class="['map-card', getTypeClass(type.value)]">
                        <div class="map-info">
                            <div class="map-name">{{ map.mapName }}</div>
                            <div class="map-id">{{ map.mapId }}</div>
                        </div>
                        <div class="map-actions">
                            <button @click.stop="editMap(map)" class="btn-edit" title="編輯">
                                ✏️
                            </button>
                            <button @click.stop="deleteMap(map)" class="btn-delete" title="刪除">
                                🗑️
                            </button>
                        </div>
                    </div>
                </div>

                <!-- 空狀態提示 -->
                <div v-if="!isCollapsed(type.value) && mapsByType[type.value]?.length === 0" class="empty-message">
                    尚未設定{{ type.name.replace(/^.+?\s/, '') }}地圖
                </div>
            </div>

            <!-- 未知地圖 -->
            <div v-if="unknownMaps.length > 0" class="map-section">
                <div class="section-header" @click="toggleCollapse('Unknown')">
                    <h3>
                        <span class="collapse-icon">{{ isCollapsed('Unknown') ? '▶' : '▼' }}</span>
                        ❓ 未分類地圖
                        <span class="count-badge">({{ unknownMaps.length }})</span>
                    </h3>
                </div>

                <div v-show="!isCollapsed('Unknown')" class="maps-grid">
                    <div v-for="map in unknownMaps"
                         :key="map.mapId"
                         class="map-card unknown">
                        <div class="map-info">
                            <div class="map-name">{{ map.mapName }}</div>
                            <div class="map-id">ID: {{ map.mapId }}</div>
                        </div>
                        <div class="map-actions">
                            <button @click.stop="editMap(map)" class="btn-edit" title="編輯">
                                ✏️
                            </button>
                            <button @click.stop="deleteMap(map)" class="btn-delete" title="刪除">
                                🗑️
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- 地圖新增/編輯對話框 -->
        <div v-if="showAddMapDialog" class="modal-overlay" @click.self="closeMapDialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3>{{ isEditingMap ? '編輯地圖' : '新增地圖' }}</h3>
                    <button @click="closeMapDialog" class="btn-close">✕</button>
                </div>

                <div class="modal-body">
                    <div class="form-group">
                        <label>地圖 ID *</label>
                        <input v-model="editingMap.mapId"
                               type="text"
                               placeholder="例如: GeBuLinCunLuo01"
                               :disabled="isEditingMap"
                               class="form-input" />
                    </div>

                    <div class="form-group">
                        <label>地圖名稱 *</label>
                        <input v-model="editingMap.mapName"
                               type="text"
                               placeholder="例如: 火炬之光"
                               class="form-input" />
                    </div>

                    <div class="form-group">
                        <label>地圖類型 *</label>
                        <select v-model="editingMap.mapType" class="form-select">
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

    const maps = ref([])
    const mapTypes = ref([])
    const showAddMapDialog = ref(false)
    const isEditingMap = ref(false)
    const notification = ref({ show: false, type: 'success', message: '' })

    const collapsedSections = ref({})

    const editingMap = ref({
        mapId: '',
        mapName: '',
        mapType: 'Netherrealm'
    })

    // 切換折疊狀態
    const toggleCollapse = (typeValue) => {
        collapsedSections.value[typeValue] = !collapsedSections.value[typeValue]
    }

    // 檢查是否已折疊（預設為 true，即收起）
    const isCollapsed = (typeValue) => {
        // 如果 collapsedSections 中沒有該類型，預設為 true（收起）
        return collapsedSections.value[typeValue] !== false
    }

    // 關閉對話框
    const closeMapDialog = () => {
        showAddMapDialog.value = false
        isEditingMap.value = false
        editingMap.value = {
            mapId: '',
            mapName: '',
            mapType: mapTypes.value.length > 0 ? mapTypes.value[0].value : 'Netherrealm'
        }
    }

    // 儲存地圖
    const saveMap = async () => {
        if (!editingMap.value.mapId || !editingMap.value.mapName) {
            showNotification('error', '地圖 ID 和名稱是必填的')
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
                closeMapDialog()
                await loadMaps()
            } else {
                showNotification('error', result.message)
            }
        } catch (error) {
            showNotification('error', '儲存地圖時發生錯誤: ' + error.message)
        }
    }

    // 顯示通知
    const showNotification = (type, message) => {
        notification.value = { show: true, type, message }
        setTimeout(() => {
            notification.value.show = false
        }, 3000)
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
                        mapId: map.id || map.mapId,
                        mapName: map.name || map.mapName,
                        mapType: map.type || map.mapType
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
        editingMap.value = { ...map }
        isEditingMap.value = true
        showAddMapDialog.value = true
    }

    // 刪除地圖
    const deleteMap = async (map) => {
        if (!confirm(`確定要刪除地圖 ${map.mapName} 嗎？`)) {
            return
        }

        try {
            const result = await apiCall('DeleteMapConfig', map.mapType, map.mapId)

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

    // Computed
    const mapsByType = computed(() => {
        const grouped = {}
        maps.value.forEach((map) => {
            const type = map.mapType || '未知'
            if (!grouped[type]) {
                grouped[type] = []
            }
            grouped[type].push(map)
        })
        return grouped
    })

    const unknownMaps = computed(() => {
        return maps.value.filter((map) => !map.mapType)
    })

    const getTypeClass = (type) => {
        const typeMap = {
            Netherrealm: 'netherrealm',
            Hideout: 'hideout',
            SecretRealm: 'secret-realm',
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
        flex-wrap: wrap;
        gap: 15px;
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
    .notification {
        padding: 15px 20px;
        border-radius: 8px;
        margin-bottom: 20px;
        font-weight: 500;
        animation: slideInDown 0.3s ease-out;
    }

    @keyframes slideInDown {
        from {
            opacity: 0;
            transform: translateY(-20px);
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

    /* 地圖區塊標題 */
    .section-header {
        display: flex;
        align-items: center;
        cursor: pointer;
        user-select: none;
        padding: 12px 15px;
        background: rgba(255, 255, 255, 0.03);
        border-radius: 8px;
        margin-bottom: 15px;
        transition: all 0.3s;
    }

        .section-header:hover {
            background: rgba(255, 255, 255, 0.06);
        }

        .section-header h3 {
            margin: 0;
            display: flex;
            align-items: center;
            gap: 10px;
            color: white;
            font-size: 1.3rem;
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

    /* 地圖網格折疊動畫 */
    .maps-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 10px;
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

    .maps-container {
        display: flex;
        flex-direction: column;
        gap: 10px;
    }

    .map-section {
        margin-bottom: 30px;
    }

    .map-card {
        position: relative;
        padding: 15px 20px;
        background: rgba(255, 255, 255, 0.05);
        border: 1px solid rgba(255, 255, 255, 0.1);
        border-radius: 8px;
        transition: all 0.3s;
    }

    .map-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
        background: rgba(255, 255, 255, 0.08);
    }

    .map-info {
        margin-bottom: 10px;
    }

    .map-name {
        color: white;
        font-size: 1.1rem;
        font-weight: 600;
        margin-bottom: 8px;
    }

    .map-id {
        font-size: 0.75rem;
        color: rgba(255, 255, 255, 0.6);        
    }

    .map-actions {
        position: absolute;
        top: 15px;
        right: 15px;
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

    .map-card.unknown {
        border-left: 4px solid #757575;
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

        /* 下拉選單選項樣式 */
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
