import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { scheduleQueryUpdate } from '@/composables/queryBatch'

function parseQuerySet(param: unknown): Set<string> {
  if (!param || typeof param !== 'string') return new Set()
  return new Set(param.split(',').filter(Boolean))
}

export function useExpandedQueryParam(queryKey: string) {
  const route = useRoute()
  const router = useRouter()

  const expanded = ref(parseQuerySet(route.query[queryKey]))

  watch(
    expanded,
    (value) => {
      const serialized = value.size > 0 ? [...value].join(',') : undefined
      scheduleQueryUpdate(router, queryKey, serialized)
    },
    { deep: true },
  )

  function toggle(id: string) {
    const newSet = new Set(expanded.value)
    if (newSet.has(id)) {
      newSet.delete(id)
    } else {
      newSet.add(id)
    }
    expanded.value = newSet
  }

  function syncFromRoute() {
    expanded.value = parseQuerySet(route.query[queryKey])
  }

  return { expanded, toggle, syncFromRoute }
}
