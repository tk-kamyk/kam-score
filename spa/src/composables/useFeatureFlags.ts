// Intentional boilerplate: no active flags today.
// The plumbing is retained so new in-development features can be gated behind
// a flag without re-introducing this infrastructure. See
// docs/requirements/feature-flags.md for usage.
import { ref } from 'vue'
import apiClient from '@/api/client'

const flags = ref<Record<string, boolean>>({})
const loaded = ref(false)
let fetchPromise: Promise<void> | null = null

export function useFeatureFlags() {
  async function fetch() {
    if (fetchPromise) return fetchPromise
    fetchPromise = (async () => {
      try {
        const { data } = await apiClient.get<Record<string, boolean>>('/feature-flags')
        flags.value = data
      } catch {
        flags.value = {}
      }
      loaded.value = true
    })()
    return fetchPromise
  }

  function isEnabled(flag: string): boolean {
    return flags.value[flag] ?? false
  }

  return { flags, loaded, isEnabled, fetch }
}
