<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useVolunteerStore } from '@/volunteer/store'
import { useSnackbar } from '@/composables/useSnackbar'
import LoadingBar from '@/components/LoadingBar.vue'
import { getErrorMessage } from '@/api/errors'
import { STATION_COLORS, stationColor } from '@/volunteer/stations'
import type { VolunteerAvailabilityDto } from '@/volunteer/types'

// Dropdown options: "No station" plus one per palette colour. `title` is the full accessible
// label in the menu; the compact closed-select label is derived inline (value + 1, or "—").
const stationItems = [
  { value: null as number | null, title: 'No station' },
  ...STATION_COLORS.map((_, i) => ({ value: i as number | null, title: `Station ${i + 1}` })),
]

const props = defineProps<{
  tournamentId: string
  shiftGroup: string
  shiftTime: string | null
}>()

const model = defineModel<boolean>()

const volunteerStore = useVolunteerStore()
const { showSuccess, showError } = useSnackbar()
const loading = ref(false)
const volunteers = ref<VolunteerAvailabilityDto[]>([])
const dirty = ref(false)

defineExpose({ dirty })

const isSpecialShift = computed(() => !props.shiftTime)

const dialogTitle = computed(() => {
  if (isSpecialShift.value) return `Assign to ${props.shiftGroup}`
  return `Assign to ${props.shiftGroup} — ${props.shiftTime}`
})

onMounted(async () => {
  await loadVolunteers()
})

async function loadVolunteers() {
  loading.value = true
  try {
    volunteers.value = await volunteerStore.fetchAvailableVolunteers(
      props.tournamentId,
      props.shiftGroup,
      props.shiftTime,
    )
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to load volunteers'))
  } finally {
    loading.value = false
  }
}

async function handleAssign(volunteerId: string) {
  try {
    await volunteerStore.assignVolunteer(
      props.tournamentId,
      props.shiftGroup,
      props.shiftTime,
      volunteerId,
    )
    await loadVolunteers()
    showSuccess('Volunteer assigned')
    dirty.value = true
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to assign volunteer'))
  }
}

async function handleUnassign(volunteerId: string) {
  try {
    await volunteerStore.unassignVolunteer(
      props.tournamentId,
      props.shiftGroup,
      props.shiftTime,
      volunteerId,
    )
    await loadVolunteers()
    showSuccess('Volunteer removed')
    dirty.value = true
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to remove volunteer'))
  }
}

async function handleSetStation(volunteerId: string, station: number | null) {
  try {
    await volunteerStore.setVolunteerStation(
      props.tournamentId,
      props.shiftGroup,
      props.shiftTime,
      volunteerId,
      station,
    )
    await loadVolunteers()
    dirty.value = true
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to set station'))
  }
}
</script>

<template>
  <v-dialog v-model="model" max-width="500">
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">
        {{ dialogTitle }}
      </v-card-title>
      <v-card-text class="pa-0">
        <LoadingBar :loading="loading" />

        <v-list density="compact">
          <v-list-item
            v-for="vol in volunteers"
            :key="vol.volunteerId"
            :class="{ 'unavailable-row': !vol.available && !isSpecialShift }"
          >
            <template #prepend>
              <v-btn
                v-if="vol.assigned"
                icon="mdi-minus-circle-outline"
                variant="text"
                size="x-small"
                color="error"
                :aria-label="'Remove ' + vol.name"
                @click.stop="handleUnassign(vol.volunteerId)"
              />
              <v-btn
                v-else
                icon="mdi-plus-circle-outline"
                variant="text"
                size="x-small"
                color="primary"
                :disabled="!vol.available && !isSpecialShift"
                :aria-label="'Assign ' + vol.name"
                @click.stop="handleAssign(vol.volunteerId)"
              />
            </template>

            <v-list-item-title>
              {{ vol.name }}
            </v-list-item-title>

            <template #append>
              <div class="d-flex align-center ga-1">
                <template v-if="!isSpecialShift">
                  <v-chip v-if="vol.playsBefore" size="x-small" variant="tonal" color="warning">
                    Plays before
                  </v-chip>
                  <v-chip v-if="vol.playsAfter" size="x-small" variant="tonal" color="warning">
                    Plays after
                  </v-chip>
                  <v-chip v-if="!vol.available" size="x-small" variant="flat" color="error">
                    Playing
                  </v-chip>
                </template>
                <v-chip size="x-small" variant="tonal" color="info">
                  {{ vol.shiftCount }} {{ vol.shiftCount === 1 ? 'shift' : 'shifts' }}
                </v-chip>
                <v-select
                  v-if="vol.assigned"
                  :model-value="vol.station ?? null"
                  :items="stationItems"
                  item-title="title"
                  item-value="value"
                  density="compact"
                  variant="outlined"
                  hide-details
                  class="station-select"
                  :aria-label="'Station for ' + vol.name"
                  @update:model-value="(val) => handleSetStation(vol.volunteerId, val)"
                >
                  <template #selection="{ item }">
                    <span class="d-flex align-center ga-1">
                      <v-icon
                        v-if="item.value != null"
                        icon="mdi-circle"
                        :color="stationColor(item.value)"
                        size="small"
                        aria-hidden="true"
                      />
                      <span class="text-caption">{{
                        item.value == null ? '—' : item.value + 1
                      }}</span>
                    </span>
                  </template>
                  <template #item="{ item, props: itemProps }">
                    <v-list-item v-bind="itemProps">
                      <template #prepend>
                        <v-icon
                          :icon="item.value == null ? 'mdi-circle-off-outline' : 'mdi-circle'"
                          :color="stationColor(item.value)"
                          size="small"
                          aria-hidden="true"
                        />
                      </template>
                    </v-list-item>
                  </template>
                </v-select>
              </div>
            </template>
          </v-list-item>
        </v-list>

        <div
          v-if="!loading && volunteers.length === 0"
          class="pa-4 text-medium-emphasis text-body-2"
        >
          No volunteers available. Add volunteers in the List tab first.
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="model = false">Close</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.unavailable-row {
  opacity: 0.5;
}

.station-select {
  width: 96px;
  min-width: 96px;
}
</style>
