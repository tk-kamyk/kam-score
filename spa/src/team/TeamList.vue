<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import SectionHeader from '@/components/SectionHeader.vue'
import ConfirmDeleteDialog from '@/components/ConfirmDeleteDialog.vue'
import TeamFormDialog from '@/team/TeamFormDialog.vue'
import GenerateSeedTeamsDialog from '@/team/GenerateSeedTeamsDialog.vue'
import TeamSchedule from '@/team/TeamSchedule.vue'
import type { TeamDto } from '@/team/types'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const route = useRoute()
const router = useRouter()
const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()

const expandedTeam = ref<string | null>((route.query.team as string) || null)

watch(expandedTeam, (teamId) => {
  const query = { ...route.query }
  if (teamId) query.team = teamId
  else delete query.team
  router.replace({ query })
})

function toggleExpand(teamId?: string) {
  if (!teamId) return
  expandedTeam.value = expandedTeam.value === teamId ? null : teamId
}

const columnCount = computed(() => {
  let count = 2
  if (props.isOwner) count += 3
  return count
})

const sortedTeams = computed(() =>
  [...teamStore.teams].sort((a, b) => (b.level ?? 0) - (a.level ?? 0)),
)

// --- Dialog state ---

const showFormDialog = ref(false)
const showDeleteDialog = ref(false)
const showGenerateDialog = ref(false)
const editingTeam = ref<TeamDto | null>(null)
const deletingTeam = ref<TeamDto | null>(null)
const formDialog = ref<InstanceType<typeof TeamFormDialog> | null>(null)
const deleteDialog = ref<InstanceType<typeof ConfirmDeleteDialog> | null>(null)
const generateDialog = ref<InstanceType<typeof GenerateSeedTeamsDialog> | null>(null)

onMounted(() => teamStore.fetchTeams(props.tournamentId))

watch(
  () => props.active,
  (isActive) => {
    if (isActive) teamStore.fetchTeams(props.tournamentId)
  },
)

function openCreate() {
  editingTeam.value = null
  showFormDialog.value = true
}

function openEdit(team: TeamDto) {
  editingTeam.value = team
  showFormDialog.value = true
}

function openDelete(team: TeamDto) {
  deletingTeam.value = team
  showDeleteDialog.value = true
}

function openGenerate() {
  showGenerateDialog.value = true
}

async function handleSave(team: TeamDto) {
  try {
    if (editingTeam.value?.id) {
      await teamStore.updateTeam(props.tournamentId, editingTeam.value.id, team)
      showSuccess('Team updated')
    } else {
      await teamStore.createTeam(props.tournamentId, team)
      showSuccess('Team created')
    }
    showFormDialog.value = false
  } catch (error) {
    if (!formDialog.value?.handleError(error)) {
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
    if (!deleteDialog.value?.handleError(error)) {
      showError('Failed to delete team')
    }
  }
}

async function handleGenerate(count: number) {
  try {
    const generated = await teamStore.generateSeedTeams(props.tournamentId, count)
    showGenerateDialog.value = false
    showSuccess(`Generated ${generated.length} seed teams`)
  } catch (error) {
    if (!generateDialog.value?.handleError(error)) {
      showError('Failed to generate seed teams')
    }
  }
}
</script>

<template>
  <div>
    <SectionHeader title="Teams">
      <div>
        <v-btn
          v-if="isOwner"
          variant="outlined"
          color="primary"
          prepend-icon="mdi-account-multiple-plus"
          class="mr-2"
          @click="openGenerate"
        >
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
          <template v-for="team in sortedTeams" :key="team.id">
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
                <v-btn
                  icon="mdi-pencil"
                  variant="text"
                  size="small"
                  :aria-label="'Edit team ' + team.name"
                  @click.stop="openEdit(team)"
                />
                <v-btn
                  icon="mdi-delete"
                  variant="text"
                  size="small"
                  color="error"
                  :aria-label="'Delete team ' + team.name"
                  @click.stop="openDelete(team)"
                />
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

    <v-alert v-else-if="!teamStore.loading" class="mt-4 mb-4" type="info" variant="tonal">
      No teams yet.
    </v-alert>

    <TeamFormDialog
      ref="formDialog"
      v-model="showFormDialog"
      :editing-team="editingTeam"
      @save="handleSave"
    />
    <ConfirmDeleteDialog
      ref="deleteDialog"
      v-model="showDeleteDialog"
      title="Delete Team"
      :message="`Are you sure you want to delete &quot;${deletingTeam?.name ?? ''}&quot;?`"
      @confirm="handleDelete"
    />
    <GenerateSeedTeamsDialog
      ref="generateDialog"
      v-model="showGenerateDialog"
      :existing-team-count="teamStore.teams.length"
      @generate="handleGenerate"
    />
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
