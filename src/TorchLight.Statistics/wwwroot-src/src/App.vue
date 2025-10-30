<template>
  <div id="app" class="app-container">
    <Header />
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import Header from './components/Header.vue'
import { onMounted } from 'vue'
import { useMapStore } from './stores/mapStore'

const mapStore = useMapStore()

onMounted(() => {
  // 設定全域事件處理器
  window.onNewMapRecord = () => {
    console.log('New map record detected')
    mapStore.refreshRecords()
  }

  window.onItemPicked = (itemName, quantity) => {
    console.log(`Item picked: ${itemName} x${quantity}`)
    mapStore.refreshCurrentMap()
  }

  // 初始載入資料
  mapStore.refreshRecords()
  mapStore.refreshCurrentMap()
})
</script>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
}

.main-content {
  flex: 1;
  overflow: auto;
  padding: 20px;
}
</style>
