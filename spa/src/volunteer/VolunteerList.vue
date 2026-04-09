<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useVolunteerStore } from '@/volunteer/store'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import SectionHeader from '@/components/SectionHeader.vue'
import type { VolunteerDto } from '@/volunteer/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  active: boolean
}>()

const volunteerStore = useVolunteerStore()
const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError, generalError } = useFormErrors()

const showFormDialog = ref(false)
const showDeleteDialog = ref(false)
const editingVolunteer = ref<VolunteerDto | null>(null)
const deletingVolunteer = ref<VolunteerDto | null>(null)
const form = ref<VolunteerDto>({ name: '', contact: null, teamId: null })
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const nameRules = [
  (v: string) => !!v || 'Volunteer name is required.',
  (v: string) => v.length <= 200 || 'Name must not exceed 200 characters.',
]

onMounted(() => {
  volunteerStore.fetchVolunteers(props.tournamentId)
  teamStore.fetchTeams(props.tournamentId)
})

watch(() => props.active, (isActive) => {
  if (isActive) {
    volunteerStore.fetchVolunteers(props.tournamentId)
    teamStore.fetchTeams(props.tournamentId)
  }
})

function openCreate() {
  editingVolunteer.value = null
  form.value = { name: '', contact: null, teamId: null }
  clearErrors()
  showFormDialog.value = true
}

function openEdit(volunteer: VolunteerDto) {
  editingVolunteer.value = volunteer
  form.value = { ...volunteer }
  clearErrors()
  showFormDialog.value = true
}

function openDelete(volunteer: VolunteerDto) {
  deletingVolunteer.value = volunteer
  clearErrors()
  showDeleteDialog.value = true
}

async function handleSave() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  try {
    if (editingVolunteer.value?.id) {
      await volunteerStore.updateVolunteer(props.tournamentId, editingVolunteer.value.id, form.value)
      showSuccess('Volunteer updated')
    } else {
      await volunteerStore.createVolunteer(props.tournamentId, form.value)
      showSuccess('Volunteer created')
    }
    showFormDialog.value = false
  } catch (error) {
    if (!handleError(error)) {
      showError(editingVolunteer.value ? 'Failed to update volunteer' : 'Failed to create volunteer')
    }
  }
}

async function handleDelete() {
  if (!deletingVolunteer.value?.id) return
  try {
    await volunteerStore.deleteVolunteer(props.tournamentId, deletingVolunteer.value.id)
    showDeleteDialog.value = false
    showSuccess('Volunteer deleted')
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to delete volunteer')
    }
  }
}

const realTeams = () => teamStore.teams.filter(t => !t.isPlaceholder)

function teamName(teamId?: string | null): string {
  if (!teamId) return '—'
  return teamStore.teams.find(t => t.id === teamId)?.name ?? '—'
}
</script>

<template>
  <div>
    <SectionHeader title="Volunteers">
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">
        Add Volunteer
      </v-btn>
    </SectionHeader>

    <v-progress-linear v-if="volunteerStore.loading" indeterminate color="primary" class="mb-4" />

    <v-card v-if="volunteerStore.volunteers.length > 0" class="data-table-card">
      <v-table density="comfortable" class="styled-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Contact</th>
            <th>Team</th>
            <th class="text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="volunteer in volunteerStore.volunteers" :key="volunteer.id">
            <td>{{ volunteer.name }}</td>
            <td class="text-medium-emphasis">{{ volunteer.contact || '—' }}</td>
            <td class="text-medium-emphasis">{{ teamName(volunteer.teamId) }}</td>
            <td class="text-right">
              <v-btn icon="mdi-pencil" variant="text" size="small" :aria-label="'Edit volunteer ' + volunteer.name" @click="openEdit(volunteer)" />
              <v-btn icon="mdi-delete" variant="text" size="small" color="error" :aria-label="'Delete volunteer ' + volunteer.name" @click="openDelete(volunteer)" />
            </td>
          </tr>
        </tbody>
      </v-table>
    </v-card>

    <v-alert v-else-if="!volunteerStore.loading" type="info" variant="tonal" class="mt-4 mb-4">
      No volunteers yet.
    </v-alert>

    <!-- Create / Edit Dialog -->
    <v-dialog v-model="showFormDialog" max-width="500">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">
          {{ editingVolunteer ? 'Edit Volunteer' : 'Add Volunteer' }}
        </v-card-title>
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          <v-form ref="formRef" @submit.prevent="handleSave">
            <v-text-field
              v-model="form.name"
              label="Name"
              :rules="nameRules"
              :error-messages="fieldErrors('name')"
              @update:model-value="clearFieldError('name')"
            />
            <v-text-field
              v-model="form.contact"
              label="Contact"
              :error-messages="fieldErrors('contact')"
              @update:model-value="clearFieldError('contact')"
            />
            <v-select
              v-model="form.teamId"
              :items="realTeams()"
              item-title="name"
              item-value="id"
              label="Team"
              clearable
              :error-messages="fieldErrors('teamId')"
              @update:model-value="clearFieldError('teamId')"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showFormDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleSave">
            {{ editingVolunteer ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Delete Volunteer</v-card-title>
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          Are you sure you want to delete "{{ deletingVolunteer?.name }}"?
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
</style>
