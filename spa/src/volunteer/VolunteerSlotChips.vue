<script setup lang="ts">
import { computed } from 'vue'
import VolunteerShiftChip from '@/volunteer/VolunteerShiftChip.vue'
import { stationColor } from '@/volunteer/stations'
import type { ShiftVolunteerDto } from '@/volunteer/types'

// Renders a slot's assigned volunteers grouped by station. The list arrives already ordered by
// station (uncoloured last) then name, so groups are just consecutive runs of the same station.
const props = defineProps<{
  volunteers: ShiftVolunteerDto[]
}>()

defineEmits<{ unassign: [volunteerId: string] }>()

const groups = computed(() => {
  const out: { station: number | null; items: ShiftVolunteerDto[] }[] = []
  for (const vol of props.volunteers) {
    const station = vol.station ?? null
    const last = out[out.length - 1]
    if (last && last.station === station) last.items.push(vol)
    else out.push({ station, items: [vol] })
  }
  return out
})
</script>

<template>
  <div v-if="volunteers.length === 0" class="text-medium-emphasis text-body-2">
    No volunteers assigned
  </div>
  <div v-else class="d-flex flex-wrap ga-4 ga-lg-8">
    <div v-for="g in groups" :key="g.station ?? -1" class="station-group">
      <div class="d-flex align-center ga-1 pl-1">
        <v-icon
          v-if="g.station != null"
          icon="mdi-circle"
          :color="stationColor(g.station)"
          size="x-small"
          aria-hidden="true"
        />
        <span class="text-caption font-weight-medium text-medium-emphasis">
          {{ g.station == null ? 'No station' : 'Station ' + (g.station + 1) }}
        </span>
      </div>
      <div class="d-flex flex-wrap ga-1 mt-1">
        <VolunteerShiftChip
          v-for="vol in g.items"
          :key="vol.volunteerId"
          :name="vol.name"
          :available="vol.available"
          @close="$emit('unassign', vol.volunteerId)"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Equal-width columns when there's room; each group wraps to its own full-width row once it
   can't hold the basis, so narrow viewports stack the groups naturally. */
.station-group {
  flex: 1 1 180px;
  min-width: 0;
}
</style>
