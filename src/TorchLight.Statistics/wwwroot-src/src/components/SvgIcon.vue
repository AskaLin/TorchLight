<template>
  <img v-if="src"
       :src="src"
       :alt="alt"
       :class="['svg-icon', className]"
       :style="iconStyle" />
  <span v-else class="svg-icon-placeholder">{{ alt }}</span>
</template>

<script setup>
  import { computed } from 'vue'

  const props = defineProps({
    name: {
      type: String,
      required: true
    },
    size: {
      type: [String, Number],
      default: '24'
    },
    color: {
      type: String,
      default: 'currentColor'
    },
    alt: {
      type: String,
      default: 'icon'
    },
    className: {
      type: String,
      default: ''
    }
  })

  const src = computed(() => {
    try {
      // ±q public/assets/icons ¸ü¤J SVG
      return `/assets/icons/${props.name}.svg`
    } catch (error) {
      console.error(`Failed to load SVG: ${props.name}`, error)
      return null
    }
  })

  const iconStyle = computed(() => ({
    width: typeof props.size === 'number' ? `${props.size}px` : props.size,
    height: typeof props.size === 'number' ? `${props.size}px` : props.size,
    fill: props.color
  }))
</script>

<style scoped>
  .svg-icon {
    display: inline-block;
    vertical-align: middle;
    transition: all 0.3s ease;
  }

  .svg-icon-placeholder {
    display: inline-block;
    width: 24px;
    height: 24px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 4px;
  }
</style>
