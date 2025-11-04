import { ref } from 'vue'

export function useNotification() {
  const notification = ref({
    show: false,
    type: 'success',
    message: ''
  })

  const showNotification = (type, message, duration = 3000) => {
    notification.value = {
      show: true,
      type,
      message
    }

    if (duration > 0) {
      setTimeout(() => {
        notification.value.show = false
      }, duration)
    }
  }

  const hideNotification = () => {
    notification.value.show = false
  }

  return {
    notification,
    showNotification,
    hideNotification
  }
}
