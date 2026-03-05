import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

function parseQuerySet(param: unknown): Set<string> {
  if (!param || typeof param !== 'string') return new Set()
  return new Set(param.split(',').filter(Boolean))
}

export function useExpandedQueryParam(queryKey: string) {
  const route = useRoute()
  const router = useRouter()

  const expanded = ref(parseQuerySet(route.query[queryKey]))

  watch(expanded, (value) => {
    const query = { ...route.query }
    if (value.size > 0) {
      query[queryKey] = [...value].join(',')
    } else {
      delete query[queryKey]
    }
    router.replace({ query })
  }, { deep: true })

  function toggle(id: string) {
    const newSet = new Set(expanded.value)
    if (newSet.has(id)) {
      newSet.delete(id)
    } else {
      newSet.add(id)
    }
    expanded.value = newSet
  }

  return { expanded, toggle }
}
