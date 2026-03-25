import { nextTick } from 'vue'
import type { Router, LocationQueryRaw } from 'vue-router'

// Singleton batching state — intentionally module-level so multiple composables
// (useExpandedQueryParam, useGroupSelection) coalesce into a single router.replace()
// per tick, avoiding duplicate navigation warnings.
let pending: Record<string, string | undefined> = {}
let flushScheduled = false
let routerRef: Router | null = null

export function scheduleQueryUpdate(router: Router, key: string, value: string | undefined) {
  routerRef = router
  pending[key] = value

  if (!flushScheduled) {
    flushScheduled = true
    nextTick(() => {
      try {
        const query: LocationQueryRaw = { ...routerRef!.currentRoute.value.query }
        for (const [k, v] of Object.entries(pending)) {
          if (v === undefined) delete query[k]
          else query[k] = v
        }
        routerRef!.replace({ query })
      } finally {
        pending = {}
        flushScheduled = false
      }
    })
  }
}
