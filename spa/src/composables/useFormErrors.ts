import { ref } from 'vue'
import { parseValidationErrors } from '@/api/errors'

export function useFormErrors() {
  const serverErrors = ref<Record<string, string[]>>({})

  function fieldErrors(field: string): string[] {
    return serverErrors.value[field] ?? []
  }

  function handleError(error: unknown): boolean {
    const parsed = parseValidationErrors(error)
    if (parsed) {
      serverErrors.value = parsed.fieldErrors
      return true
    }
    return false
  }

  function clearErrors() {
    serverErrors.value = {}
  }

  function clearFieldError(field: string) {
    const { [field]: _, ...rest } = serverErrors.value
    serverErrors.value = rest
  }

  return { fieldErrors, handleError, clearErrors, clearFieldError }
}
