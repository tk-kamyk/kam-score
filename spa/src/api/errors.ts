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
  return parseErrorDetail(error) ?? fallback
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
