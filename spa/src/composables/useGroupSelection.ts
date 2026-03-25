import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { scheduleQueryUpdate } from '@/composables/queryBatch'

function parseGroupSelections(param: unknown): Map<string, string> {
  if (!param || typeof param !== 'string') return new Map()
  const map = new Map<string, string>()
  for (const entry of param.split(',').filter(Boolean)) {
    const [phaseId, groupId] = entry.split(':')
    if (phaseId && groupId) map.set(phaseId, groupId)
  }
  return map
}

export function useGroupSelection(queryKey = 'group') {
  const route = useRoute()
  const router = useRouter()

  const selectedGroups = ref(parseGroupSelections(route.query[queryKey]))

  watch(selectedGroups, (groups) => {
    const serialized = groups.size > 0 ? [...groups.entries()].map(([p, g]) => `${p}:${g}`).join(',') : undefined
    scheduleQueryUpdate(router, queryKey, serialized)
  }, { deep: true })

  function selectGroup(phaseId: string, groupId: string) {
    const newMap = new Map(selectedGroups.value)
    newMap.set(phaseId, groupId)
    selectedGroups.value = newMap
  }

  return { selectedGroups, selectGroup }
}
