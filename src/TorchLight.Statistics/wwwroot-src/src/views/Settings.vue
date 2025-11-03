<template>
    <div class="settings-page">
        <!-- Tab 切換 -->
        <div class="tabs">
            <button :class="['tab-btn', { active: activeTab === 'maps' }]"
                    @click="activeTab = 'maps'">
                地圖設定
            </button>
            <button :class="['tab-btn', { active: activeTab === 'statistics' }]"
                    @click="activeTab = 'statistics'">
                拾取物品設定
            </button>
            <button :class="['tab-btn', { active: activeTab === 'executeLine' }]"
                    @click="activeTab = 'executeLine'">
                斬殺線
            </button>
        </div>

        <!-- Tab 內容 -->
        <div class="tab-content">
            <div v-show="activeTab === 'maps'" class="tab-panel">
                <MapSettingsPanel />
            </div>

            <div v-show="activeTab === 'statistics'" class="tab-panel">
                <PickupStatisticsSettings />
            </div>

            <div v-show="activeTab === 'executeLine'" class="tab-panel">
                <ExecuteLineSettings />
            </div>
        </div>
    </div>
</template>

<script setup>
    import { ref } from 'vue'
    import MapSettingsPanel from '../components/MapSettingsPanel.vue'
    import PickupStatisticsSettings from '../components/PickupStatisticsSettings.vue'
    import ExecuteLineSettings from '../components/ExecuteLineSettings.vue'

    const activeTab = ref('maps')
</script>

<style scoped>
    .settings-page {
        max-width: 1400px;
        margin: 0 auto;
        padding: 20px;
        background: transparent;
    }

    /* Tab 樣式 */
    .tabs {
        display: flex;
        gap: 10px;
        margin-bottom: 25px;
        border-bottom: 2px solid rgba(255, 255, 255, 0.1);
    }

    .tab-btn {
        padding: 12px 24px;
        background: none;
        border: none;
        color: rgba(255, 255, 255, 0.6);
        font-size: 1.1rem;
        cursor: pointer;
        transition: all 0.3s;
        border-bottom: 3px solid transparent;
        position: relative;
        top: 2px;
    }

        .tab-btn:hover {
            color: rgba(255, 255, 255, 0.9);
            background: rgba(255, 255, 255, 0.05);
        }

        .tab-btn.active {
            color: white;
            border-bottom-color: #667eea;
            background: rgba(102, 126, 234, 0.1);
        }

    .tab-content {
        animation: fadeIn 0.3s ease-in-out;
    }

    @keyframes fadeIn {
        from {
            opacity: 0;
            transform: translateY(10px);
        }

        to {
            opacity: 1;
            transform: translateY(0);
        }
    }

    .tab-panel {
        min-height: 400px;
        background: transparent;
        border: none;
        padding: 0;
    }
</style>
