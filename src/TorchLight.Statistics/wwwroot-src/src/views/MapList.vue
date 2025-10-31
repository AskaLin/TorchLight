<template>
    <div class="map-list">
        <div class="list-header">
            <h2>地圖記錄列表</h2>
            <button @click="refreshList" class="btn-refresh" :disabled="mapStore.loading">
                <span v-if="!mapStore.loading">🔄 重新載入</span>
                <span v-else>載入中...</span>
            </button>
        </div>

        <div v-if="mapStore.loading && mapStore.mapRecords.length === 0" class="loading">
            載入中...
        </div>

        <div v-else-if="mapStore.mapRecords.length === 0" class="empty">
            <p>目前沒有地圖記錄</p>
            <p>進入遊戲的異界地圖後，系統會自動開始記錄</p>
        </div>

        <div v-else class="records-grid">
            <div v-for="record in sortedRecords"
                 :key="record.recordId"
                 class="record-card"
                 @click="goToDetail(record.recordId)">
                <div class="record-header">
                    <h3>{{ record.name }}</h3>
                    <span class="record-time">{{ formatDateTime(record.startTime) }}</span>
                </div>

                <div class="record-info">
                    <div v-if="record.mapTicket" class="info-item">
                        <span class="label">🎟️ 門票:</span>
                        <span class="value">{{ record.mapTicket }}</span>
                    </div>

                    <div v-if="record.compass && record.compass.length > 0" class="info-item">
                        <span class="label">🧭 羅盤:</span>
                        <span class="value">{{ record.compass.join(', ') }}</span>
                    </div>

                    <div v-if="record.probe" class="info-item">
                        <span class="label">📍 探針:</span>
                        <span class="value">{{ record.probe }}</span>
                    </div>
                </div>

                <div class="record-stats">
                    <div class="stat-item">
                        <span class="stat-label">用時</span>
                        <span class="stat-value">{{ record.useTime }}</span>
                    </div>
                    <div class="stat-item">
                        <span class="stat-label">物品種類</span>
                        <span class="stat-value">{{ record.itemCount }}</span>
                    </div>
                    <div class="stat-item">
                        <span class="stat-label">總數量</span>
                        <span class="stat-value">{{ record.totalQuantity }}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
    import { computed } from 'vue'
    import { useRouter } from 'vue-router'
    import { useMapStore } from '../stores/mapStore'

    const router = useRouter()
    const mapStore = useMapStore()

    const sortedRecords = computed(() => {
        return [...mapStore.mapRecords].sort((a, b) =>
            new Date(b.startTime) - new Date(a.startTime)
        )
    })

    const refreshList = () => {
        mapStore.refreshRecords()
    }

    const goToDetail = (recordId) => {
        router.push(`/maps/${recordId}`)
    }

    const formatDateTime = (dateStr) => {
        const date = new Date(dateStr)
        return date.toLocaleString('zh-TW', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            hour12: false, // 👈 關鍵：使用 24 小時制
        })
    }
</script>

<style scoped>
    .map-list {
        max-width: 1400px;
        margin: 0 auto;
    }

    .list-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 25px;
    }

        .list-header h2 {
            color: white;
            margin: 0;
        }

    .btn-refresh {
        padding: 10px 20px;
        background: rgba(255, 255, 255, 0.1);
        border: 2px solid rgba(255, 255, 255, 0.2);
        border-radius: 8px;
        color: white;
        cursor: pointer;
        transition: all 0.3s;
    }

        .btn-refresh:hover:not(:disabled) {
            background: rgba(255, 255, 255, 0.2);
            transform: translateY(-2px);
        }

        .btn-refresh:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }

    .loading, .empty {
        text-align: center;
        padding: 60px 20px;
        color: rgba(255, 255, 255, 0.7);
        background: rgba(255, 255, 255, 0.05);
        border-radius: 12px;
    }

    .records-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
        gap: 20px;
    }

    .record-card {
        background: rgba(255, 255, 255, 0.05);
        backdrop-filter: blur(10px);
        border: 2px solid rgba(255, 255, 255, 0.1);
        border-radius: 12px;
        padding: 20px;
        cursor: pointer;
        transition: all 0.3s;
    }

        .record-card:hover {
            border-color: rgba(255, 255, 255, 0.3);
            transform: translateY(-5px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.3);
        }

    .record-header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        margin-bottom: 15px;
        border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        padding-bottom: 10px;
    }

        .record-header h3 {
            color: white;
            margin: 0;
            font-size: 1.2rem;
        }

    .record-time {
        color: rgba(255, 255, 255, 0.6);
        font-size: 0.9rem;
    }

    .record-info {
        display: flex;
        flex-direction: column;
        gap: 8px;
        margin-bottom: 15px;
    }

    .info-item {
        display: flex;
        gap: 10px;
        color: rgba(255, 255, 255, 0.8);
        font-size: 0.9rem;
    }

    .label {
        font-weight: 600;
    }

    .value {
        opacity: 0.8;
    }

    .record-stats {
        display: flex;
        justify-content: space-around;
        padding-top: 15px;
        border-top: 1px solid rgba(255, 255, 255, 0.1);
    }

    .stat-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 5px;
    }

    .stat-label {
        color: rgba(255, 255, 255, 0.6);
        font-size: 0.85rem;
    }

    .stat-value {
        color: white;
        font-size: 1.1rem;
        font-weight: 600;
    }
</style>
