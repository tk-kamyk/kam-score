import { ref } from 'vue'

const SUCCESS_TIMEOUT_MS = 3000
const ERROR_TIMEOUT_MS = 10000

const show = ref(false)
const message = ref('')
const color = ref('success')
const timeout = ref(SUCCESS_TIMEOUT_MS)

export function useSnackbar() {
  function showSuccess(msg: string) {
    message.value = msg
    color.value = 'success'
    timeout.value = SUCCESS_TIMEOUT_MS
    show.value = true
  }

  function showError(msg: string) {
    message.value = msg
    color.value = 'error'
    timeout.value = ERROR_TIMEOUT_MS
    show.value = true
  }

  return { show, message, color, timeout, showSuccess, showError }
}
