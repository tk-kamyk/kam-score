<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import type { TeamDto } from '@/team/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
}>()

const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError } = useFormErrors()

const showFormDialog = ref(false)
const showDeleteDialog = ref(false)
const editingTeam = ref<TeamDto | null>(null)
const deletingTeam = ref<TeamDto | null>(null)
const form = ref<TeamDto>({ name: '', level: 50 })
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const nameRules = [
  (v: string) => !!v || 'Team name is required.',
  (v: string) => v.length <= 100 || 'Team name must not exceed 100 characters.',
]

const emailRules = [
  (v: string | null | undefined) => !v || /.+@.+\..+/.test(v) || 'Email must be a valid email address.',
]

const phoneRules = [
  (v: string | null | undefined) => !v || /^\+?[\d\s\-()]{7,20}$/.test(v) || 'Phone must be a valid phone number.',
]

onMounted(() => {
  teamStore.fetchTeams(props.tournamentId)
})

function openCreate() {
  editingTeam.value = null
  form.value = { name: '', level: 50, email: null, phone: null }
  clearErrors()
  showFormDialog.value = true
}

function openEdit(team: TeamDto) {
  editingTeam.value = team
  form.value = { ...team }
  clearErrors()
  showFormDialog.value = true
}

function openDelete(team: TeamDto) {
  deletingTeam.value = team
  showDeleteDialog.value = true
}

async function handleSave() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  try {
    if (editingTeam.value?.id) {
      await teamStore.updateTeam(props.tournamentId, editingTeam.value.id, form.value)
      showSuccess('Team updated')
    } else {
      await teamStore.createTeam(props.tournamentId, form.value)
      showSuccess('Team created')
    }
    showFormDialog.value = false
  } catch (error) {
    if (!handleError(error)) {
      showError(editingTeam.value ? 'Failed to update team' : 'Failed to create team')
    }
  }
}

async function handleDelete() {
  if (!deletingTeam.value?.id) return
  try {
    await teamStore.deleteTeam(props.tournamentId, deletingTeam.value.id)
    showDeleteDialog.value = false
    showSuccess('Team deleted')
  } catch {
    showError('Failed to delete team')
  }
}
</script>

<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <h3 class="section-title text-title-small text-md-title-medium">Teams</h3>
      <v-btn v-if="isOwner" color="primary" prepend-icon="mdi-plus" @click="openCreate">
        Add Team
      </v-btn>
    </div>

    <v-progress-linear v-if="teamStore.loading" indeterminate color="primary" class="mb-4" />

    <v-card v-if="teamStore.teams.length > 0" class="data-table-card">
      <v-table density="comfortable" class="styled-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Level</th>
            <th v-if="isOwner">Email</th>
            <th v-if="isOwner">Phone</th>
            <th v-if="isOwner" class="text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="team in teamStore.teams" :key="team.id">
            <td>{{ team.name }}</td>
            <td>{{ team.level }}</td>
            <td v-if="isOwner">{{ team.email ?? '—' }}</td>
            <td v-if="isOwner">{{ team.phone ?? '—' }}</td>
            <td v-if="isOwner" class="text-right">
              <v-btn icon="mdi-pencil" variant="text" size="small" @click="openEdit(team)" />
              <v-btn icon="mdi-delete" variant="text" size="small" color="error" @click="openDelete(team)" />
            </td>
          </tr>
        </tbody>
      </v-table>
    </v-card>

    <v-alert class="mt-4 mb-4" v-else-if="!teamStore.loading" type="info" variant="tonal">
      No teams yet.
    </v-alert>

    <!-- Create / Edit Dialog -->
    <v-dialog v-model="showFormDialog" max-width="500">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">
          {{ editingTeam ? 'Edit Team' : 'Add Team' }}
        </v-card-title>
        <v-card-text>
          <v-form ref="formRef" @submit.prevent="handleSave">
            <v-text-field
              v-model="form.name"
              label="Name"
              :rules="nameRules"
              :error-messages="fieldErrors('name')"
              @update:model-value="clearFieldError('name')"
            />
            <v-slider
              v-model="form.level"
              label="Level"
              :min="0"
              :max="100"
              :step="1"
              thumb-label="always"
              color="primary"
              class="mt-4"
            />
            <v-text-field
              v-model="form.email"
              label="Email"
              type="email"
              :rules="emailRules"
              :error-messages="fieldErrors('email')"
              @update:model-value="clearFieldError('email')"
            />
            <v-text-field
              v-model="form.phone"
              label="Phone"
              :rules="phoneRules"
              :error-messages="fieldErrors('phone')"
              @update:model-value="clearFieldError('phone')"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showFormDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleSave">
            {{ editingTeam ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Delete Team</v-card-title>
        <v-card-text>
          Are you sure you want to delete "{{ deletingTeam?.name }}"?
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.data-table-card {
    border: 1px solid var(--ks-border);
}

.styled-table thead tr {
    background-color: rgb(var(--v-theme-surface-bright));
}

.styled-table tbody tr:hover {
    background-color: var(--ks-border-subtle);
}
</style>
