<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useVolunteerStore } from '@/volunteer/store'
import { useSnackbar } from '@/composables/useSnackbar'
import VolunteerAssignDialog from '@/volunteer/VolunteerAssignDialog.vue'

const props = defineProps<{
  tournamentId: string
}>()

const volunteerStore = useVolunteerStore()
const { showSuccess, showError } = useSnackbar()

const showAssignDialog = ref(false)
const selectedShiftGroup = ref('')
const selectedShiftTime = ref<string | null>(null)

onMounted(() => {
  volunteerStore.fetchShifts(props.tournamentId)
})

function openAssignDialog(groupName: string, shiftTime?: string | null) {
  selectedShiftGroup.value = groupName
  selectedShiftTime.value = shiftTime ?? null
  showAssignDialog.value = true
}

async function handleAssigned() {
  showAssignDialog.value = false
  await volunteerStore.fetchShifts(props.tournamentId)
}

async function handleUnassign(groupName: string, shiftTime: string | null | undefined, volunteerId: string) {
  try {
    await volunteerStore.unassignVolunteer(props.tournamentId, groupName, shiftTime ?? null, volunteerId)
    await volunteerStore.fetchShifts(props.tournamentId)
    showSuccess('Volunteer removed from shift')
  } catch {
    showError('Failed to remove volunteer')
  }
}
</script>

<template>
  <div>
    <v-progress-linear v-if="volunteerStore.shiftsLoading" indeterminate color="primary" class="mb-4" />

    <div class="shift-groups">
      <v-card v-for="group in volunteerStore.shiftGroups" :key="group.name" class="mb-4 shift-group-card">
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
                {{ shift.shiftTime ?? '' }}
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

    <v-alert v-if="!volunteerStore.shiftsLoading && volunteerStore.shiftGroups.length === 0" type="info" variant="tonal" class="mt-4 mb-4">
      No phases defined yet. Set up the tournament structure first.
    </v-alert>

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
