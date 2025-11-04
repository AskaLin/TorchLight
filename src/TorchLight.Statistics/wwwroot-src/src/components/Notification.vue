<template>
  <Transition name="notification-slide">
    <div v-if="show"
         :class="['notification-float', type]">
      {{ message }}
    </div>
  </Transition>
</template>

<script setup>
defineProps({
  show: {
    type: Boolean,
    default: false
  },
  type: {
    type: String,
    default: 'success',
    validator: (value) => ['success', 'error', 'info', 'warning'].includes(value)
  },
  message: {
    type: String,
    default: ''
  }
})
</script>

<style scoped>
/* 浮動通知 - 固定在頂部中央 */
.notification-float {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 9999;
  padding: 15px 30px;
  border-radius: 8px;
  font-weight: 500;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(10px);
  min-width: 300px;
  max-width: 500px;
  text-align: center;
}

.notification-float.success {
  background: rgba(76, 175, 80, 0.95);
  border: 1px solid #4caf50;
  color: white;
}

.notification-float.error {
  background: rgba(244, 67, 54, 0.95);
  border: 1px solid #f44336;
  color: white;
}

.notification-float.info {
  background: rgba(33, 150, 243, 0.95);
  border: 1px solid #2196f3;
  color: white;
}

.notification-float.warning {
  background: rgba(255, 152, 0, 0.95);
  border: 1px solid #ff9800;
  color: white;
}

/* 通知動畫 - 從上方滑入 */
.notification-slide-enter-active {
  animation: slideInDown 0.3s ease-out;
}

.notification-slide-leave-active {
  animation: slideOutUp 0.3s ease-in;
}

@keyframes slideInDown {
  from {
    opacity: 0;
    transform: translateX(-50%) translateY(-20px);
  }

  to {
    opacity: 1;
    transform: translateX(-50%) translateY(0);
  }
}

@keyframes slideOutUp {
  from {
    opacity: 1;
    transform: translateX(-50%) translateY(0);
  }

  to {
    opacity: 0;
    transform: translateX(-50%) translateY(-20px);
  }
}
</style>
