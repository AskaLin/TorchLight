<template>
  <div class="history-records">
  <h2>📚 歷史紀錄</h2>

    <div v-if="loading" class="loading">載入中...</div>

    <div v-else-if="error" class="error">{{ error }}</div>

    <div v-else-if="records.length === 0" class="empty">
      <p>尚無歷史紀錄</p>
    </div>

    <div v-else class="records-list">
      <div v-for="record in records" :key="record.fileName" class="record-card">
 <div class="record-header">
        <h3>🗓️ {{ record.recordTime }}</h3>
     <span class="map-count">{{ record.totalMaps }} 張地圖</span>
        </div>

        <div class="record-stats">
          <div class="stat-item">
            <span class="stat-label">總數量:</span>
      <span class="stat-value">{{ record.totalQuantity }}</span>
          </div>
          <div class="stat-item">
  <span class="stat-label">遊戲時間:</span>
       <span class="stat-value">{{ record.totalPlayTime }}</span>
</div>
        </div>

  <div class="top-items">
          <h4>前 10 名拾取物品</h4>
          <div class="items-grid">
            <div v-for="item in record.topItems" :key="item.baseId" class="item-chip">
   <span class="item-name">{{ item.name }}</span>
    <span class="item-quantity">{{ item.totalQuantity }}</span>
        <span v-if="item.like > 0" class="item-like">❤️ {{ item.like }}</span>
            </div>
    </div>
        </div>

        <div class="record-actions">
     <button @click="viewDetail(record)" class="btn-detail">查看詳細紀錄</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { apiCall } from '../utils/api'

  const router = useRouter()
  const records = ref([])
  const loading = ref(true)
  const error = ref(null)

  onMounted(async () => {
    await loadHistoryRecords()
  })

  const loadHistoryRecords = async () => {
    loading.value = true
    error.value = null
    try {
      const data = await apiCall('GetHistoryRecords')
      if (data && !data.error) {
     records.value = data
      } else {
   error.value = data?.error || '載入失敗'
      }
    } catch (err) {
  error.value = err.message
      console.error('Failed to load history records:', err)
  } finally {
      loading.value = false
    }
  }

  const viewDetail = async (record) => {
    try {
      console.log('🔍 Requesting history detail for file:', record.fileName)
      const detail = await apiCall('GetHistoryRecordDetail', record.fileName)
      
      console.log('📦 Received detail response:', detail)
    
      if (detail && !detail.error) {
        console.log('✅ History detail loaded successfully')
        console.log('  - Total maps:', detail.summary?.totalMaps)
  console.log('  - Records count:', detail.records?.length)
     
        // 跳轉到歷史記錄的地圖列表視圖
     router.push({
      path: '/history/detail',
          query: {
   fileName: record.fileName
          },
       state: {
            historyData: detail
   }
        })
      } else {
   console.error('❌ Failed to load history detail:', detail?.error)
        alert(`載入失敗：${detail?.error || '未知錯誤'}`)
      }
    } catch (err) {
      console.error('💥 Exception while loading history detail:', err)
      alert(`載入失敗：${err.message}`)
    }
  }
</script>

<style scoped>
  .history-records {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
  }

    .history-records h2 {
      color: white;
      margin-bottom: 30px;
      font-size: 2rem;
    }

  .loading,
  .error,
  .empty {
    text-align: center;
    padding: 40px;
    color: rgba(255, 255, 255, 0.7);
font-size: 1.2rem;
  }

  .error {
    color: #f44336;
  }

  .records-list {
    display: flex;
    flex-direction: column;
    gap: 20px;
  }

  .record-card {
    background: rgba(255, 255, 255, 0.05);
    backdrop-filter: blur(10px);
    border-radius: 12px;
    padding: 25px;
    border: 1px solid rgba(255, 255, 255, 0.1);
    transition: all 0.3s;
  }

    .record-card:hover {
      border-color: rgba(255, 255, 255, 0.3);
      box-shadow: 0 8px 20px rgba(0, 0, 0, 0.3);
      transform: translateY(-2px);
    }

  .record-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

    .record-header h3 {
      color: white;
margin: 0;
      font-size: 1.5rem;
    }

.map-count {
    color: #2196f3;
    font-size: 1.1rem;
    font-weight: 600;
  }

  .record-stats {
    display: flex;
    gap: 30px;
    margin-bottom: 20px;
    padding: 15px;
  background: rgba(0, 0, 0, 0.2);
    border-radius: 8px;
  }

  .stat-item {
    display: flex;
    gap: 10px;
  }

  .stat-label {
    color: rgba(255, 255, 255, 0.6);
  }

.stat-value {
    color: #ffd700;
    font-weight: 600;
  }

  .top-items {
  margin-bottom: 20px;
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

  .record-actions {
    display: flex;
    justify-content: flex-end;
  }

  .btn-detail {
    padding: 10px 20px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 1rem;
    font-weight: 600;
    transition: all 0.3s;
  }

    .btn-detail:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 15px rgba(102, 126, 234, 0.5);
    }

    .btn-detail:active {
 transform: translateY(0);
    }
</style>
