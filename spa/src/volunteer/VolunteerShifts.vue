<script setup lang="ts">
import { ref } from 'vue'
import VolunteerAssignDialog from '@/volunteer/VolunteerAssignDialog.vue'
import type { ShiftGroupDto } from '@/volunteer/types'

const props = defineProps<{
  tournamentId: string
}>()

const showAssignDialog = ref(false)
const selectedShiftGroup = ref('')
const selectedShiftTime = ref<string | null>(null)

// Mock data for Gate 3
const shiftGroups = ref<ShiftGroupDto[]>([
  {
    name: 'Set-up',
    isSpecial: true,
    shifts: [{ shiftTime: null, volunteers: [{ volunteerId: '1', name: 'John Doe', available: true }] }],
  },
  {
    name: 'Pool',
    isSpecial: false,
    shifts: [
      {
        shiftTime: '09:00',
        volunteers: [
          { volunteerId: '1', name: 'John Doe', available: true },
          { volunteerId: '2', name: 'Jane Doe', available: false },
        ],
      },
      { shiftTime: '09:20', volunteers: [] },
      {
        shiftTime: '09:40',
        volunteers: [{ volunteerId: '3', name: 'Bob Smith', available: true }],
      },
    ],
  },
  {
    name: 'Playoffs',
    isSpecial: false,
    shifts: [
      { shiftTime: '11:00', volunteers: [] },
      { shiftTime: '11:20', volunteers: [] },
    ],
  },
  {
    name: 'Cleanup',
    isSpecial: true,
    shifts: [{ shiftTime: null, volunteers: [] }],
  },
])

const loading = ref(false)

function formatTime(shiftTime?: string | null): string {
  if (!shiftTime) return ''
  return shiftTime
}

function openAssignDialog(groupName: string, shiftTime?: string | null) {
  selectedShiftGroup.value = groupName
  selectedShiftTime.value = shiftTime ?? null
  showAssignDialog.value = true
}

function handleAssigned() {
  showAssignDialog.value = false
  // Will reload shifts from API in Gate 6
}

function handleUnassign(groupName: string, shiftTime: string | null | undefined, volunteerId: string) {
  // Will call API in Gate 6
  const group = shiftGroups.value.find(g => g.name === groupName)
  if (!group) return
  const shift = group.shifts.find(s => s.shiftTime === (shiftTime ?? null))
  if (!shift) return
  shift.volunteers = shift.volunteers.filter(v => v.volunteerId !== volunteerId)
}
</script>

<template>
  <div>
    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4" />

    <div class="shift-groups">
      <v-card v-for="group in shiftGroups" :key="group.name" class="mb-4 shift-group-card">
        <v-card-title class="text-subtitle-1 font-weight-bold bg-surface-bright py-2 px-4">
          {{ group.name }}
        </v-card-title>

        <v-table density="compact" class="shift-table">
          <thead>
            <tr>
              <th v-if="!group.isSpecial" class="time-col">Time</th>
              <th>Assigned Volunteers</th>
              <th class="text-right actions-col">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(shift, index) in group.shifts" :key="shift.shiftTime ?? index">
              <td v-if="!group.isSpecial" class="time-col">
                {{ formatTime(shift.shiftTime) }}
              </td>
              <td>
                <div class="d-flex flex-wrap ga-1">
                  <v-chip
                    v-for="vol in shift.volunteers"
                    :key="vol.volunteerId"
                    size="small"
                    :color="vol.available ? 'primary' : 'warning'"
                    :variant="vol.available ? 'tonal' : 'outlined'"
                    closable
                    @click:close="handleUnassign(group.name, shift.shiftTime, vol.volunteerId)"
                  >
                    {{ vol.name }}
                    <v-icon v-if="!vol.available" end icon="mdi-alert" size="x-small" />
                  </v-chip>
                  <span v-if="shift.volunteers.length === 0" class="text-medium-emphasis text-body-2">
                    No volunteers assigned
                  </span>
                </div>
              </td>
              <td class="text-right actions-col">
                <v-btn
                  icon="mdi-plus"
                  variant="text"
                  size="small"
                  color="primary"
                  :aria-label="'Assign volunteer to ' + group.name + (shift.shiftTime ? ' at ' + shift.shiftTime : '')"
                  @click="openAssignDialog(group.name, shift.shiftTime)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </div>

    <VolunteerAssignDialog
      v-if="showAssignDialog"
      v-model="showAssignDialog"
      :tournament-id="tournamentId"
      :shift-group="selectedShiftGroup"
      :shift-time="selectedShiftTime"
      @assigned="handleAssigned"
    />
  </div>
</template>

<style scoped>
.shift-group-card {
  border: 1px solid var(--ks-border);
}

.shift-table {
  background: transparent;
}

.time-col {
  width: 80px;
}

.actions-col {
  width: 60px;
}
</style>
