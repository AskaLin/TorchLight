<template>
  <div class="history-detail">
    <div class="detail-header">
      <button @click="goBack" class="btn-back">← 返回歷史紀錄</button>
      <h2>📚 {{ historyTitle }}</h2>
    </div>

    <div v-if="loading" class="loading">載入中...</div>

    <div v-else-if="error" class="error">
      <p>❌ {{ error }}</p>
      <p class="debug-info">檔案名稱: {{ route.query.fileName || '未提供' }}</p>
      <button @click="goBack" class="btn-retry">返回列表</button>
    </div>

    <div v-else-if="!historyData" class="error">
      <p>找不到歷史記錄資料</p>
      <button @click="goBack" class="btn-retry">返回列表</button>
    </div>

    <div v-else class="detail-content">
      <!-- 統計摘要 -->
      <div class="summary-card">
        <h3>統計摘要</h3>
        <div class="summary-stats">
          <div class="stat-item">
            <span class="stat-label">總地圖數：</span>
            <span class="stat-value">{{ historyData.summary.totalMaps }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">物品種類：</span>
            <span class="stat-value">{{ historyData.summary.totalItems }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">總數量：</span>
            <span class="stat-value">{{ historyData.summary.totalQuantity }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">遊戲時間：</span>
            <span class="stat-value">{{ historyData.summary.totalPlayTime }}</span>
          </div>
        </div>

        <div v-if="historyData.summary.mostPickedItems && historyData.summary.mostPickedItems.length > 0" class="top-items">
          <h4>最常拾取物品 Top 10</h4>
          <div class="items-grid">
            <div v-for="item in historyData.summary.mostPickedItems" :key="item.baseId" class="item-chip">
              <span class="item-name">{{ item.name }}</span>
              <span class="item-quantity">{{ item.totalQuantity }}</span>
              <span v-if="item.like > 0" class="item-like">❤️ {{ item.like }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 地圖記錄列表 -->
      <div class="maps-section">
        <h3>地圖記錄列表</h3>
        <div class="records-grid">
          <div v-for="record in sortedRecords"
               :key="record.recordId"
               class="record-card"
               @click="viewMapDetail(record)">
            <div class="record-header">
              <h4>{{ record.name }}</h4>
              <span class="record-time">{{ formatDateTime(record.startTime) }}</span>
            </div>

            <div class="record-info">
              <div v-if="record.mapTicket" class="info-item">
                <span class="label">🎟️ 門票:</span>
                <span class="value">{{ record.mapTicket }}</span>
              </div>

              <div v-if="record.compass && record.compass.length > 0" class="info-item">
                <span class="label">🧭 羅盤:</span>
                <span class="value">{{ record.compass.filter(c => c).join(', ') }}</span>
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
                <span class="stat-value">{{ getItemCount(record) }}</span>
              </div>
              <div class="stat-item">
                <span class="stat-label">總數量</span>
                <span class="stat-value">{{ getTotalQuantity(record) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { useRouter, useRoute } from 'vue-router'
  import { apiCall } from '../utils/api'

  const router = useRouter()
  const route = useRoute()

  const loading = ref(true)
  const historyData = ref(null)
  const error = ref(null)

  const historyTitle = computed(() => {
    if (!historyData.value?.records?.length) {
      return '歷史記錄詳情'
    }
    const firstRecord = historyData.value.records[0]
    const date = new Date(firstRecord.startTime)
    return `${date.getMonth() + 1}/${date.getDate()} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')} 的記錄`
  })

  const sortedRecords = computed(() => {
    if (!historyData.value?.records) return []
    return [...historyData.value.records].sort((a, b) =>
      new Date(b.startTime) - new Date(a.startTime)
    )
  })

  onMounted(async () => {
    loading.value = true
    error.value = null

    try {
      if (route.state?.historyData) {
        historyData.value = route.state.historyData
      } else if (route.query.fileName) {
        const detail = await apiCall('GetHistoryRecordDetail', route.query.fileName)
        if (detail && !detail.error) {
          historyData.value = detail
        } else {
          error.value = detail?.error || '載入歷史記錄失敗'
        }
      } else {
        error.value = '缺少必要的參數'
      }
    } catch (err) {
      error.value = `載入失敗：${err.message}`
    } finally {
      loading.value = false
    }
  })

  const goBack = () => {
    router.push('/history')
  }

  const viewMapDetail = (record) => {
    try {
      sessionStorage.setItem('historyRecord', JSON.stringify(record))
      sessionStorage.setItem('historyData', JSON.stringify(historyData.value))
    } catch (error) {
      console.error('Failed to save to sessionStorage:', error)
    }

    router.push({
      name: 'map-detail',
      params: { id: record.recordId },
      query: {
        fromHistory: 'true',
        fileName: route.query.fileName
      }
    })
  }

  const formatDateTime = (dateStr) => {
    const date = new Date(dateStr)
    return date.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })
  }

  const getItemCount = (record) => {
    return record.pickRecord ? Object.keys(record.pickRecord).length : 0
  }

  const getTotalQuantity = (record) => {
    if (!record.pickRecord) return 0
    return Object.values(record.pickRecord).reduce((sum, item) => sum + item.total, 0)
  }
</script>

<style scoped>
  .history-detail {
    max-width: 1400px;
    margin: 0 auto;
    padding: 20px;
  }

  .detail-header {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-bottom: 30px;
  }

    .detail-header h2 {
      color: white;
      margin: 0;
      font-size: 1.8rem;
    }

  .btn-back {
    padding: 10px 20px;
    background: rgba(255, 255, 255, 0.1);
    border: 2px solid rgba(255, 255, 255, 0.2);
    border-radius: 8px;
    color: white;
    cursor: pointer;
    transition: all 0.3s;
    white-space: nowrap;
  }

    .btn-back:hover {
      background: rgba(255, 255, 255, 0.2);
      transform: translateX(-3px);
    }

  .loading,
  .error {
    text-align: center;
    padding: 60px 20px;
    color: rgba(255, 255, 255, 0.7);
    background: rgba(255, 255, 255, 0.05);
    border-radius: 12px;
  }

  .error {
    color: #f44336;
  }

  .debug-info {
    margin-top: 10px;
    font-size: 0.9rem;
    color: rgba(255, 255, 255, 0.5);
  }

  .btn-retry {
    margin-top: 20px;
    padding: 10px 20px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border: none;
    border-radius: 8px;
    color: white;
    cursor: pointer;
    font-size: 1rem;
    transition: all 0.3s;
  }

    .btn-retry:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

  .summary-card {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border-radius: 12px;
    padding: 25px;
    margin-bottom: 30px;
    border: 1px solid rgba(255, 255, 255, 0.1);
  }

    .summary-card h3,
    .maps-section h3 {
      color: white;
      margin: 0 0 20px 0;
      font-size: 1.3rem;
    }

  .summary-stats {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 15px;
    margin-bottom: 20px;
  }

  .stat-item {
    display: flex;
    gap: 10px;
    align-items: center;
  }

  .stat-label {
    color: rgba(255, 255, 255, 0.6);
  }

  .stat-value {
    color: #ffd700;
    font-weight: 600;
    font-size: 1.1rem;
  }

  .top-items {
    margin-top: 20px;
    padding-top: 20px;
    border-top: 1px solid rgba(255, 255, 255, 0.1);
  }

    .top-items h4 {
      color: rgba(255, 255, 255, 0.8);
      margin-bottom: 15px;
      font-size: 1rem;
    }

  .items-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
  }

  .item-chip {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 12px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 20px;
    font-size: 0.9rem;
  }

  .item-name {
    color: white;
  }

  .item-quantity {
    color: #4caf50;
    font-weight: 600;
  }

  .item-like {
    color: #ff9800;
    font-size: 0.85rem;
  }

  .maps-section {
    margin-top: 30px;
  }

    .maps-section h3 {
      color: white;
      margin-bottom: 20px;
    }

  .records-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
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

    .record-header h4 {
      color: white;
      margin: 0;
      font-size: 1.1rem;
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

    .record-stats .stat-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 5px;
    }

    .record-stats .stat-label {
      color: rgba(255, 255, 255, 0.6);
      font-size: 0.85rem;
    }

    .record-stats .stat-value {
      color: white;
      font-size: 1.1rem;
      font-weight: 600;
    }
</style>
