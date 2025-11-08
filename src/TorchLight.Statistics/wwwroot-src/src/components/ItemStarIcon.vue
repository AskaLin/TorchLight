<template>
  <img v-if="like > 0"
       :src="getStarIcon()"
       alt="star"
       class="item-star-icon"
       :class="{ clickable: clickable }"
       :style="{ opacity: opacity, cursor: clickable ? 'pointer' : 'default' }"
       @click="handleClick" />
</template>

<script setup>
  import { computed } from 'vue'

  const props = defineProps({
    like: {
      type: Number,
      default: 0
    },
    clickable: {
      type: Boolean,
      default: false
    },
    opacity: {
      type: Number,
      default: 1
    }
  })

  const emit = defineEmits(['click'])

  const getStarIcon = () => {
    // ½T«O like ­È¦b 0-6 ½d³ò¤º
    const safeLike = Math.max(0, Math.min(6, props.like))
    return `/assets/icons/star-${safeLike}.svg`
  }

  const handleClick = (event) => {
    if (props.clickable) {
      emit('click', event)
    }
  }
</script>

<style scoped>
  .item-star-icon {
    width: 20px;
    height: 20px;
    flex-shrink: 0;
    filter: drop-shadow(0 0 4px rgba(255, 215, 0, 0.5));
    transition: all 0.3s;
    vertical-align: middle;
    margin-right: 4px;
  }

    .item-star-icon.clickable {
      cursor: pointer;
    }

      .item-star-icon.clickable:hover {
        transform: scale(1.2) rotate(20deg);
        filter: drop-shadow(0 0 8px rgba(255, 215, 0, 0.8));
      }

      .item-star-icon.clickable:active {
        transform: scale(1.1) rotate(10deg);
      }
</style>
