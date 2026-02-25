import { ref } from 'vue'

const show = ref(false)
const message = ref('')
const color = ref('success')

export function useSnackbar() {
  function showSuccess(msg: string) {
    message.value = msg
    color.value = 'success'
    show.value = true
  }

  function showError(msg: string) {
    message.value = msg
    color.value = 'error'
    show.value = true
  }

  return { show, message, color, showSuccess, showError }
}
