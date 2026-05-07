import type { AxiosError } from 'axios'

export const COLD_START_MESSAGE = 'The application is starting up — please try again in a moment.'

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

export function isTimeoutError(error: unknown): boolean {
  const axiosError = error as AxiosError
  return axiosError?.code === 'ECONNABORTED' || axiosError?.code === 'ETIMEDOUT'
}

export function parseErrorDetail(error: unknown): string | null {
  const axiosError = error as AxiosError<ProblemDetails>
  const data = axiosError?.response?.data
  if (!data?.detail || axiosError?.response?.status === 400) return null
  return data.detail
}

export function getErrorMessage(error: unknown, fallback: string): string {
  if (isTimeoutError(error)) return COLD_START_MESSAGE

  const validation = parseValidationErrors(error)
  if (validation) {
    const messages = Object.values(validation.fieldErrors).flat()
    const hasCustomDetail = validation.message !== 'Validation failed'
    if (messages.length > 0) {
      const header = hasCustomDetail ? validation.message : fallback
      const bullets = messages.map((m) => `• ${m}`).join('\n')
      return `${header}\n${bullets}`
    }
    if (hasCustomDetail) return validation.message
  }

  const detail = parseErrorDetail(error)
  if (detail) return detail

  const axiosError = error as AxiosError<ProblemDetails>
  const title = axiosError?.response?.data?.title
  if (title) return title

  return fallback
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
