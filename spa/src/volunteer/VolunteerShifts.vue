<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useVolunteerStore } from '@/volunteer/store'
import { useSnackbar } from '@/composables/useSnackbar'
import VolunteerAssignDialog from '@/volunteer/VolunteerAssignDialog.vue'
import VolunteerAutoAssignDialog from '@/volunteer/VolunteerAutoAssignDialog.vue'
import VolunteerSlotChips from '@/volunteer/VolunteerSlotChips.vue'
import ConfirmDeleteDialog from '@/components/ConfirmDeleteDialog.vue'
import LoadingBar from '@/components/LoadingBar.vue'
import { getErrorMessage } from '@/api/errors'

const props = defineProps<{
  tournamentId: string
}>()

const volunteerStore = useVolunteerStore()
const { showSuccess, showError } = useSnackbar()

const showAssignDialog = ref(false)
const selectedShiftGroup = ref('')
const selectedShiftTime = ref<string | null>(null)
const assignDialogRef = ref<InstanceType<typeof VolunteerAssignDialog> | null>(null)

const autoAssignTarget = ref<string | null>(null)
const clearTarget = ref<string | null>(null)
const autoAssignDialogRef = ref<InstanceType<typeof VolunteerAutoAssignDialog> | null>(null)

watch(showAssignDialog, (open) => {
  if (!open && assignDialogRef.value?.dirty) {
    volunteerStore.fetchShifts(props.tournamentId)
  }
})

onMounted(() => {
  volunteerStore.fetchShifts(props.tournamentId)
})

function openAssignDialog(groupName: string, shiftTime?: string | null) {
  selectedShiftGroup.value = groupName
  selectedShiftTime.value = shiftTime ?? null
  showAssignDialog.value = true
}

async function handleUnassign(
  groupName: string,
  shiftTime: string | null | undefined,
  volunteerId: string,
) {
  try {
    // The store action refreshes shifts internally — no need to fetch again here.
    await volunteerStore.unassignVolunteer(
      props.tournamentId,
      groupName,
      shiftTime ?? null,
      volunteerId,
    )
    showSuccess('Volunteer removed from shift')
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to remove volunteer'))
  }
}

async function handleAutoAssign(payload: {
  volunteersPerShift: number
  stationCount: number | null
}) {
  const target = autoAssignTarget.value
  if (!target) return
  try {
    await volunteerStore.autoAssignShiftGroup(
      props.tournamentId,
      target,
      payload.volunteersPerShift,
      payload.stationCount,
    )
    showSuccess(`Auto-assigned volunteers to ${target}`)
    autoAssignTarget.value = null
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to auto-assign volunteers'))
    autoAssignDialogRef.value?.handleError(error)
  }
}

async function handleClearConfirm() {
  const target = clearTarget.value
  if (!target) return
  try {
    await volunteerStore.clearShiftGroupAssignments(props.tournamentId, target)
    showSuccess(`Cleared all assignments for ${target}`)
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to clear assignments'))
  } finally {
    clearTarget.value = null
  }
}
</script>

<template>
  <div>
    <LoadingBar :loading="volunteerStore.shiftsLoading" class="mb-4" />

    <div class="shift-groups">
      <v-card
        v-for="group in volunteerStore.shiftGroups"
        :key="group.name"
        class="mb-4 shift-group-card"
      >
        <v-card-title
          class="d-flex align-center text-subtitle-1 font-weight-bold bg-surface-bright py-2 px-4"
        >
          <span class="flex-grow-1">{{ group.name }}</span>
          <v-btn
            icon="mdi-auto-fix"
            variant="text"
            size="small"
            color="primary"
            :aria-label="'Auto-assign volunteers to ' + group.name"
            @click="autoAssignTarget = group.name"
          />
          <v-btn
            icon="mdi-broom"
            variant="text"
            size="small"
            color="error"
            :aria-label="'Clear all assignments for ' + group.name"
            @click="clearTarget = group.name"
          />
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
                <VolunteerSlotChips
                  :volunteers="shift.volunteers"
                  @unassign="(id) => handleUnassign(group.name, shift.shiftTime, id)"
                />
              </td>
              <td class="text-right actions-col">
                <v-btn
                  icon="mdi-plus"
                  variant="text"
                  size="small"
                  color="primary"
                  :aria-label="
                    'Assign volunteer to ' +
                    group.name +
                    (shift.shiftTime ? ' at ' + shift.shiftTime : '')
                  "
                  @click="openAssignDialog(group.name, shift.shiftTime)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </div>

    <v-alert
      v-if="!volunteerStore.shiftsLoading && volunteerStore.shiftGroups.length === 0"
      type="info"
      variant="tonal"
      class="mt-4 mb-4"
    >
      No phases defined yet. Set up the tournament structure first.
    </v-alert>

    <VolunteerAssignDialog
      v-if="showAssignDialog"
      ref="assignDialogRef"
      v-model="showAssignDialog"
      :tournament-id="tournamentId"
      :shift-group="selectedShiftGroup"
      :shift-time="selectedShiftTime"
    />

    <VolunteerAutoAssignDialog
      v-if="autoAssignTarget"
      ref="autoAssignDialogRef"
      :model-value="!!autoAssignTarget"
      :shift-group="autoAssignTarget"
      @update:model-value="(open) => !open && (autoAssignTarget = null)"
      @confirm="handleAutoAssign"
    />

    <ConfirmDeleteDialog
      v-if="clearTarget"
      :model-value="!!clearTarget"
      title="Clear assignments"
      :message="`Remove every volunteer assignment from ${clearTarget}? This cannot be undone.`"
      confirm-label="Clear"
      @update:model-value="(open) => !open && (clearTarget = null)"
      @confirm="handleClearConfirm"
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
