import { ref } from 'vue'
import { parseValidationErrors, parseErrorDetail } from '@/api/errors'

export function useFormErrors() {
  const serverErrors = ref<Record<string, string[]>>({})
  const generalError = ref<string | null>(null)

  function fieldErrors(field: string): string[] {
    return serverErrors.value[field] ?? []
  }

  function handleError(error: unknown): boolean {
    clearErrors()

    const parsed = parseValidationErrors(error)
    if (parsed) {
      serverErrors.value = parsed.fieldErrors
      generalError.value = parsed.message
      return true
    }

    const detail = parseErrorDetail(error)
    if (detail) {
      generalError.value = detail
      return true
    }

    return false
  }

  function clearErrors() {
    serverErrors.value = {}
    generalError.value = null
  }

  function clearFieldError(field: string) {
    const { [field]: _, ...rest } = serverErrors.value
    serverErrors.value = rest
  }

  return { fieldErrors, handleError, clearErrors, clearFieldError, generalError }
}
