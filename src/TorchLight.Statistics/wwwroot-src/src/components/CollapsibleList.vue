<template>
  <div class="collapsible-list">
    <!-- 動態渲染大分類區塊 -->
    <div v-for="section in sections"
         :key="section.key"
         class="list-section">
      <!-- 大分類標題與折疊按鈕 -->
      <div class="section-header" @click="toggleCollapse(section.key)">
        <h3>
          <span class="collapse-icon">{{ isCollapsed(section.key) ? '▶' : '▼' }}</span>
          {{ section.name }}
          <span class="count-badge">({{ section.totalCount }})</span>
        </h3>
      </div>

      <!-- 內容區塊（可折疊） -->
      <div v-show="!isCollapsed(section.key)" class="section-content">
        <!-- 如果有子分類 -->
        <template v-if="section.subcategories && section.subcategories.length > 0">
          <div v-for="subcategory in section.subcategories"
               :key="`${section.key}-${subcategory.key}`"
               class="subcategory-section">
            <!-- 小分類標題 -->
            <div class="subcategory-header"
                 @click="toggleSubcategoryCollapse(section.key, subcategory.key)">
              <h4>
                <span class="collapse-icon">
                  {{ isSubcategoryCollapsed(section.key, subcategory.key) ? '▶' : '▼' }}
                </span>
                {{ subcategory.name }}
                <span class="count-badge">({{ subcategory.items.length }})</span>
              </h4>
            </div>

            <!-- 子分類項目網格（slot） -->
            <div v-show="!isSubcategoryCollapsed(section.key, subcategory.key)"
                 class="items-grid">
              <slot name="item"
                    :items="subcategory.items"
                    :section="section"
                    :subcategory="subcategory">
              </slot>
            </div>
          </div>
        </template>

        <!-- 如果沒有子分類，直接顯示項目 -->
        <template v-else>
          <div class="items-grid">
            <slot name="item"
                  :items="section.items"
                  :section="section">
            </slot>
          </div>
        </template>

        <!-- 空狀態提示 -->
        <div v-if="section.totalCount === 0" class="empty-message">
          <slot name="empty" :section="section">
            尚未設定{{ section.name.replace(/^.+?\s/, '') }}項目
          </slot>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref } from 'vue'

  /**
   * Props 定義
   * @property {Array} sections - 大分類陣列
   *   - key: 唯一識別碼
   *   - name: 顯示名稱（包含 emoji）
   *   - totalCount: 總項目數
   *   - items: 項目陣列（如果沒有子分類）
   *   - subcategories: 子分類陣列（可選）
   *     - key: 子分類唯一識別碼
   *     - name: 子分類顯示名稱
   *     - items: 子分類項目陣列
   */
  const props = defineProps({
    sections: {
      type: Array,
      required: true,
      default: () => []
    }
  })

  // 大分類折疊狀態
  const collapsedSections = ref({})

  // 小分類折疊狀態
  const collapsedSubcategories = ref({})

  // 切換大分類折疊狀態
  const toggleCollapse = (key) => {
    collapsedSections.value[key] = !collapsedSections.value[key]
  }

  // 檢查大分類是否已折疊（預設為 true，即折疊）
  const isCollapsed = (key) => {
    return collapsedSections.value[key] !== false
  }

  // 切換小分類折疊狀態
  const toggleSubcategoryCollapse = (sectionKey, subcategoryKey) => {
    const key = `${sectionKey}-${subcategoryKey}`
    collapsedSubcategories.value[key] = !collapsedSubcategories.value[key]
  }

  // 檢查小分類是否已折疊（預設為 true，即折疊）
  const isSubcategoryCollapsed = (sectionKey, subcategoryKey) => {
    const key = `${sectionKey}-${subcategoryKey}`
    return collapsedSubcategories.value[key] !== false
  }
</script>

<style scoped>
  .collapsible-list {
    width: 100%;
  }

  /* 大分類區塊 */
  .list-section {
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

  /* 內容區塊容器 */
  .section-content {
    padding-left: 20px;
    animation: slideDown 0.3s ease-out;
  }

  /* 小分類區塊 */
  .subcategory-section {
    margin-bottom: 20px;
  }

  /* 小分類標題 */
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

  /* 折疊圖示 */
  .collapse-icon {
    font-size: 0.9rem;
    transition: transform 0.3s;
    display: inline-block;
    width: 20px;
    color: rgba(255, 255, 255, 0.6);
  }

  /* 數量徽章 */
  .count-badge {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.5);
    font-weight: normal;
  }

  /* 項目網格容器 */
  .items-grid {
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

  /* 空狀態提示 */
  .empty-message {
    text-align: center;
    padding: 30px;
    color: rgba(255, 255, 255, 0.5);
    background: rgba(255, 255, 255, 0.05);
    border-radius: 8px;
    margin-left: 20px;
  }

  /* 響應式設計 */
  @media (max-width: 1200px) {
    .items-grid {
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
    }
  }

  @media (max-width: 768px) {
    .items-grid {
      grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
      padding-left: 10px;
    }

    .section-content {
      padding-left: 10px;
    }
  }

  @media (max-width: 480px) {
    .items-grid {
      grid-template-columns: 1fr;
    }
  }
</style>
