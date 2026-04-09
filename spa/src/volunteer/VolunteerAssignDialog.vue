<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import type { VolunteerAvailabilityDto } from '@/volunteer/types'

const props = defineProps<{
  tournamentId: string
  shiftGroup: string
  shiftTime: string | null
}>()

const emit = defineEmits<{
  assigned: []
}>()

const model = defineModel<boolean>()

const loading = ref(false)

// Mock data for Gate 3
const volunteers = ref<VolunteerAvailabilityDto[]>([
  { volunteerId: '3', name: 'Alice Wonder', shiftCount: 0, available: true, playsBefore: false, playsAfter: false, assigned: false },
  { volunteerId: '4', name: 'Bob Smith', shiftCount: 1, available: true, playsBefore: false, playsAfter: true, assigned: false },
  { volunteerId: '1', name: 'Charlie Brown', shiftCount: 2, available: true, playsBefore: true, playsAfter: false, assigned: true },
  { volunteerId: '2', name: 'Dave Jones', shiftCount: 1, available: false, playsBefore: false, playsAfter: false, assigned: false },
])

const isSpecialShift = computed(() => !props.shiftTime)

const dialogTitle = computed(() => {
  if (isSpecialShift.value) return `Assign to ${props.shiftGroup}`
  return `Assign to ${props.shiftGroup} — ${props.shiftTime}`
})

async function handleAssign(volunteerId: string) {
  // Will call API in Gate 6
  const vol = volunteers.value.find(v => v.volunteerId === volunteerId)
  if (vol) vol.assigned = true
  emit('assigned')
}

async function handleUnassign(volunteerId: string) {
  // Will call API in Gate 6
  const vol = volunteers.value.find(v => v.volunteerId === volunteerId)
  if (vol) vol.assigned = false
}
</script>

<template>
  <v-dialog v-model="model" max-width="500">
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">
        {{ dialogTitle }}
      </v-card-title>
      <v-card-text class="pa-0">
        <v-progress-linear v-if="loading" indeterminate color="primary" />

        <v-list density="compact">
          <v-list-item
            v-for="vol in volunteers"
            :key="vol.volunteerId"
            :class="{ 'unavailable-row': !vol.available && !isSpecialShift }"
          >
            <template #prepend>
              <v-icon
                v-if="vol.assigned"
                icon="mdi-check-circle"
                color="primary"
                size="small"
              />
              <v-icon
                v-else
                icon="mdi-circle-outline"
                color="grey"
                size="small"
              />
            </template>

            <v-list-item-title>
              {{ vol.name }}
            </v-list-item-title>

            <template #append>
              <div class="d-flex align-center ga-1">
                <v-chip size="x-small" variant="tonal" color="info">
                  {{ vol.shiftCount }} {{ vol.shiftCount === 1 ? 'shift' : 'shifts' }}
                </v-chip>
                <template v-if="!isSpecialShift">
                  <v-chip v-if="vol.playsBefore" size="x-small" variant="tonal" color="warning">
                    plays before
                  </v-chip>
                  <v-chip v-if="vol.playsAfter" size="x-small" variant="tonal" color="warning">
                    plays after
                  </v-chip>
                  <v-chip v-if="!vol.available" size="x-small" variant="flat" color="error">
                    playing
                  </v-chip>
                </template>
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
              </div>
            </template>
          </v-list-item>
        </v-list>
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
</style>
