import type { AxiosError } from 'axios'

export interface ValidationErrors {
  fieldErrors: Record<string, string[]>
  message: string
}

interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

function toCamelCase(str: string): string {
  if (!str) return str
  return str.charAt(0).toLowerCase() + str.slice(1)
}

export function parseValidationErrors(error: unknown): ValidationErrors | null {
  const axiosError = error as AxiosError<ProblemDetails>
  const data = axiosError?.response?.data

  if (!data || axiosError?.response?.status !== 400) {
    return null
  }

  const fieldErrors: Record<string, string[]> = {}

  if (data.errors) {
    for (const [key, messages] of Object.entries(data.errors)) {
      fieldErrors[toCamelCase(key)] = messages
    }
  }

  return {
    fieldErrors,
    message: data.detail ?? 'Validation failed',
  }
}
