<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import SectionHeader from '@/components/SectionHeader.vue'
import TeamSchedule from '@/team/TeamSchedule.vue'
import type { TeamDto } from '@/team/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const route = useRoute()
const router = useRouter()
const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError, generalError } = useFormErrors()


const expandedTeam = ref<string | null>((route.query.team as string) || null)

watch(expandedTeam, (teamId) => {
  const query = { ...route.query }
  if (teamId) {
    query.team = teamId
  } else {
    delete query.team
  }
  router.replace({ query })
})

function toggleExpand(teamId?: string) {
  if (!teamId) return
  expandedTeam.value = expandedTeam.value === teamId ? null : teamId
}

const columnCount = computed(() => {
  let count = 2 // Name + Level
  if (props.isOwner) count += 3 // Email + Phone + Actions
  return count
})

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

watch(() => props.active, (isActive) => {
  if (isActive) teamStore.fetchTeams(props.tournamentId)
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
  clearErrors()
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
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to delete team')
    }
  }
}

const showGenerateDialog = ref(false)
const seedCount = ref(8)

const seedCountRules = [
  (v: number) => (v >= 1 && v <= 100) || 'Count must be between 1 and 100.',
]

const seedPreview = computed(() => {
  const start = teamStore.teams.length + 1
  const count = seedCount.value
  if (count < 1 || count > 100) return ''
  const names = Array.from({ length: Math.min(count, 4) }, (_, i) => `Seed ${start + i}`)
  if (count > 4) names.push(`... Seed ${start + count - 1}`)
  return names.join(', ')
})

function openGenerate() {
  seedCount.value = 8
  clearErrors()
  showGenerateDialog.value = true
}

async function handleGenerate() {
  if (seedCount.value < 1 || seedCount.value > 100) return
  try {
    const generated = await teamStore.generateSeedTeams(props.tournamentId, seedCount.value)
    showGenerateDialog.value = false
    showSuccess(`Generated ${generated.length} seed teams`)
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to generate seed teams')
    }
  }
}
</script>

<template>
  <div>
    <SectionHeader title="Teams">
      <div>
        <v-btn v-if="isOwner" variant="outlined" color="primary" prepend-icon="mdi-account-multiple-plus" class="mr-2" @click="openGenerate">
          Generate Teams
        </v-btn>
        <v-btn v-if="isOwner" color="primary" prepend-icon="mdi-plus" @click="openCreate">
          Add Team
        </v-btn>
      </div>
    </SectionHeader>

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
          <template v-for="team in teamStore.teams" :key="team.id">
            <tr class="team-row" @click="toggleExpand(team.id)">
              <td>
                <v-icon
                  :icon="expandedTeam === team.id ? 'mdi-chevron-down' : 'mdi-chevron-right'"
                  size="small"
                  class="mr-1"
                />
                {{ team.name }}
              </td>
              <td>{{ team.level }}</td>
              <td v-if="isOwner">{{ team.email ?? '—' }}</td>
              <td v-if="isOwner">{{ team.phone ?? '—' }}</td>
              <td v-if="isOwner" class="text-right">
                <v-btn icon="mdi-pencil" variant="text" size="small" :aria-label="'Edit team ' + team.name" @click.stop="openEdit(team)" />
                <v-btn icon="mdi-delete" variant="text" size="small" color="error" :aria-label="'Delete team ' + team.name" @click.stop="openDelete(team)" />
              </td>
            </tr>
            <tr v-if="expandedTeam === team.id">
              <td :colspan="columnCount" class="pa-0 team-expanded-cell">
                <TeamSchedule
                  :tournament-id="tournamentId"
                  :team-id="team.id!"
                  :team-name="team.name"
                  :is-owner="isOwner"
                />
              </td>
            </tr>
          </template>
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
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          Are you sure you want to delete "{{ deletingTeam?.name }}"?
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Generate Seed Teams Dialog -->
    <v-dialog v-model="showGenerateDialog" max-width="450">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Generate Seed Teams</v-card-title>
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          <v-text-field
            v-model.number="seedCount"
            label="Number of teams"
            type="number"
            :min="1"
            :max="100"
            :rules="seedCountRules"
          />
          <div v-if="seedPreview" class="text-body-2 text-medium-emphasis mt-1">
            Will generate: {{ seedPreview }}
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showGenerateDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleGenerate">Generate</v-btn>
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

.styled-table tbody tr.team-row {
    cursor: pointer;
}

.styled-table tbody tr.team-row:hover {
    background-color: var(--ks-border-subtle);
}

.team-expanded-cell {
    background-color: rgb(var(--v-theme-surface-bright));
}
</style>
