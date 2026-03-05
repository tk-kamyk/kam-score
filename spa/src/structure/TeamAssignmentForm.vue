<script setup lang="ts">
import { computed, ref } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
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
    .map(id => props.teams.find(t => t.id === id))
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

const availableTeams = computed(() => {
  return props.teams
    .filter(t => {
      if (!t.id || assignedTeamIdsInPhase.value.has(t.id)) return false
      if (props.phaseOrder > 1) {
        // Phase 2+: only show placeholder teams from the previous phase
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
  } catch {
    showError('Failed to assign team')
  }
}

async function handleRemove(teamId: string) {
  try {
    await structureStore.removeTeam(
      props.tournamentId,
      props.phaseId,
      props.group.id!,
      teamId,
    )
    showSuccess('Team removed')
    await structureStore.fetchStructure(props.tournamentId)
  } catch {
    showError('Failed to remove team')
  }
}
</script>

<template>
  <div>
    <v-list v-if="assignedTeams.length > 0" density="compact" class="pa-0 team-list">
      <v-list-item v-for="team in assignedTeams" :key="team.id" class="px-0">
        <template #default>
          <span class="text-body-medium" :class="{ 'text-italic text-medium-emphasis': team.isPlaceholder }">
            {{ team.name }}
          </span>
        </template>
        <template v-if="editing" #append>
          <v-btn
            icon="mdi-close"
            variant="text"
            size="x-small"
            color="error"
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
        placeholder="Select team..."
        hide-details
        class="flex-grow-1"
      />
      <v-btn
        icon="mdi-plus"
        size="small"
        color="primary"
        variant="tonal"
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
