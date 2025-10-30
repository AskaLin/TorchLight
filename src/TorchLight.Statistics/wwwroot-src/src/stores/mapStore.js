import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { apiCall } from '../utils/api'

export const useMapStore = defineStore('map', () => {
  const mapRecords = ref([])
  const currentMapInfo = ref({ isInMap: false, mapName: '' })
  const loading = ref(false)
  const error = ref(null)
  
  // 新增：監控狀態
  const monitoringStatus = ref('待機中')
  const lastBagSyncTime = ref(null)

  // 初始化 WebView2 訊息監聽
  if (typeof window !== 'undefined') {
    window.addEventListener('message', (event) => {
      try {
   const message = typeof event.data === 'string' ? JSON.parse(event.data) : event.data

        if (message && message.type) {
    handleBackendMessage(message)
   }
      } catch (err) {
        console.error('Failed to parse message:', err)
      }
    })
  }

  // 處理來自後端的訊息
  const handleBackendMessage = (message) => {
    console.log('Received message from backend:', message)
    
    switch (message.type) {
      case 'logMonitoringStatus':
        if (message.data && message.data.status) {
   monitoringStatus.value = message.data.status
 console.log('Monitoring status updated:', message.data.status)
     }
        break
        
      case 'bagSyncStatus':
   if (message.data && message.data.syncTime) {
          lastBagSyncTime.value = new Date(message.data.syncTime)
      console.log('Bag sync time updated:', lastBagSyncTime.value)
        }
        break
    
      case 'newMapRecord':
        refreshRecords()
        break
        
      case 'itemPicked':
        // 可以在這裡更新即時拾取資訊
        console.log('Item picked:', message.data)
        break
    }
  }

  // 獲取所有地圖記錄
  const refreshRecords = async () => {
    loading.value = true
    error.value = null
    try {
      const data = await apiCall('GetMapRecords')
      mapRecords.value = data
    } catch (err) {
      error.value = err.message
      console.error('Failed to refresh records:', err)
    } finally {
      loading.value = false
    }
  }

  // 獲取當前地圖資訊
  const refreshCurrentMap = async () => {
    try {
      const data = await apiCall('GetCurrentMapInfo')
      currentMapInfo.value = data
  } catch (err) {
    console.error('Failed to refresh current map:', err)
    }
  }

  // 獲取地圖詳情
  const getMapDetail = async (recordId) => {
    loading.value = true
    error.value = null
 try {
      const data = await apiCall('GetMapRecordDetail', recordId)
      return data
    } catch (err) {
      error.value = err.message
      console.error('Failed to get map detail:', err)
      return null
    } finally {
      loading.value = false
    }
  }

  // 清除所有記錄
  const clearAllRecords = async () => {
    try {
      const result = await apiCall('ClearAllRecords')
      if (result.success) {
    mapRecords.value = []
        return true
      }
      return false
    } catch (err) {
      console.error('Failed to clear records:', err)
      return false
    }
  }

  // 匯出記錄
  const exportRecords = async () => {
    try {
      const data = await apiCall('ExportRecordsJson')
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
 const a = document.createElement('a')
      a.href = url
a.download = `torchlight-records-${new Date().toISOString().slice(0, 10)}.json`
      a.click()
      URL.revokeObjectURL(url)
      return true
  } catch (err) {
      console.error('Failed to export records:', err)
      return false
    }
  }

  // Computed
const totalMaps = computed(() => mapRecords.value.length)
  const totalItems = computed(() => mapRecords.value.reduce((sum, r) => sum + (r.itemCount || 0), 0))

  return {
    mapRecords,
    currentMapInfo,
    loading,
    error,
    monitoringStatus,
    lastBagSyncTime,
    refreshRecords,
    refreshCurrentMap,
    getMapDetail,
    clearAllRecords,
    exportRecords,
    totalMaps,
    totalItems
  }
})
