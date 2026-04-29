<script setup lang="ts">
import { computed, ref } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { getErrorMessage } from '@/api/errors'
import type { GroupDto } from '@/structure/types'
import type { TeamDto } from '@/team/types'

const props = defineProps<{
  tournamentId: string
  phaseId: string
  group: GroupDto
  teams: TeamDto[]
  editing: boolean
  allGroups: GroupDto[]
  phaseOrder: number
  previousPhaseId?: string
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()

const selectedTeamId = ref<string | null>(null)

const assignedTeams = computed(() => {
  return (props.group.teamIds ?? [])
    .map((id) => props.teams.find((t) => t.id === id))
    .filter((t): t is TeamDto => t != null)
})

const assignedTeamIdsInPhase = computed(() => {
  const ids = new Set<string>()
  for (const g of props.allGroups) {
    for (const teamId of g.teamIds ?? []) {
      ids.add(teamId)
    }
  }
  return ids
})

const previousPhasePlaceholders = computed(() => {
  if (props.phaseOrder <= 1 || !props.previousPhaseId) return []
  return props.teams.filter((t) => t.isPlaceholder && t.sourcePhaseId === props.previousPhaseId)
})

const placeholdersResolved = computed(() => {
  const ph = previousPhasePlaceholders.value
  return ph.length > 0 && ph.every((t) => t.resolvedTeamId)
})

const resolvedTeamIds = computed(() => {
  if (!placeholdersResolved.value) return new Set<string>()
  return new Set(previousPhasePlaceholders.value.map((pt) => pt.resolvedTeamId!))
})

const availableTeams = computed(() => {
  return props.teams
    .filter((t) => {
      if (!t.id || assignedTeamIdsInPhase.value.has(t.id)) return false
      if (props.phaseOrder > 1) {
        if (placeholdersResolved.value) {
          // Previous phase completed: show real teams that placeholders resolved to
          return !t.isPlaceholder && resolvedTeamIds.value.has(t.id)
        }
        // Previous phase not completed: show placeholders
        return t.isPlaceholder && t.sourcePhaseId === props.previousPhaseId
      }
      // Phase 1: only show real teams
      return !t.isPlaceholder
    })
    .sort((a, b) => {
      // Sort placeholders by seed number
      if (a.isPlaceholder && b.isPlaceholder) return (a.seed ?? 0) - (b.seed ?? 0)
      return 0
    })
})

async function handleAssign() {
  if (!selectedTeamId.value) return
  try {
    await structureStore.assignTeam(
      props.tournamentId,
      props.phaseId,
      props.group.id!,
      selectedTeamId.value,
    )
    selectedTeamId.value = null
    showSuccess('Team assigned')
    await structureStore.fetchStructure(props.tournamentId)
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to assign team'))
  }
}

async function handleRemove(teamId: string) {
  try {
    await structureStore.removeTeam(props.tournamentId, props.phaseId, props.group.id!, teamId)
    showSuccess('Team removed')
    await structureStore.fetchStructure(props.tournamentId)
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to remove team'))
  }
}
</script>

<template>
  <div>
    <v-list v-if="assignedTeams.length > 0" density="compact" class="pa-0 team-list">
      <v-list-item v-for="team in assignedTeams" :key="team.id" class="px-0">
        <template #default>
          <span
            class="text-body-medium"
            :class="{ 'text-italic text-medium-emphasis': team.isPlaceholder }"
          >
            {{ team.name }}
          </span>
        </template>
        <template v-if="editing" #append>
          <v-btn
            icon="mdi-close"
            variant="text"
            size="x-small"
            color="error"
            :aria-label="'Remove ' + team.name"
            @click="handleRemove(team.id!)"
          />
        </template>
      </v-list-item>
    </v-list>

    <div v-else class="text-body-medium text-medium-emphasis">No teams assigned</div>

    <div v-if="editing && availableTeams.length > 0" class="d-flex align-center mt-2 gap-2">
      <v-select
        v-model="selectedTeamId"
        :items="availableTeams"
        item-title="name"
        item-value="id"
        density="compact"
        variant="outlined"
        label="Add team"
        hide-details
        class="flex-grow-1"
      />
      <v-btn
        icon="mdi-plus"
        size="small"
        color="primary"
        variant="tonal"
        aria-label="Assign selected team"
        :disabled="!selectedTeamId"
        @click="handleAssign"
      />
    </div>
  </div>
</template>

<style scoped>
.gap-2 {
  gap: 8px;
}

.team-list {
  background-color: rgb(var(--v-theme-surface-bright));
}
</style>
