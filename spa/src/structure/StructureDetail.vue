<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useTeamStore } from '@/team/store'
import { useGameStore } from '@/game/store'
import { useGamesByPhase } from '@/composables/useGamesByPhase'
import { useSnackbar } from '@/composables/useSnackbar'
import { getErrorMessage } from '@/api/errors'
import { useExpandedQueryParam } from '@/composables/useExpandedQueryParam'
import SectionHeader from '@/components/SectionHeader.vue'
import StructurePhaseCard from '@/structure/StructurePhaseCard.vue'
import PhaseForm from '@/structure/PhaseForm.vue'
import LoadingBar from '@/components/LoadingBar.vue'
import type { PhaseDto } from '@/structure/types'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const structureStore = useStructureStore()
const teamStore = useTeamStore()
const gameStore = useGameStore()
const { phaseGames } = useGamesByPhase()
const { showSuccess, showError } = useSnackbar()
const {
  expanded: expandedPhases,
  toggle: togglePhase,
  syncFromRoute,
} = useExpandedQueryParam('phase')

const showPhaseForm = ref(false)
const editingPhase = ref<PhaseDto | null>(null)

const phases = computed(() => structureStore.structure?.phases ?? [])

const hasGamesForEditing = computed(() => {
  const id = editingPhase.value?.id
  return !!id && phaseGames(id).length > 0
})

onMounted(async () => {
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    teamStore.fetchTeams(props.tournamentId),
    teamStore.fetchPlaceholders(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])
})

watch(
  () => props.active,
  async (isActive) => {
    if (!isActive) return
    syncFromRoute()
    await Promise.all([
      structureStore.fetchStructure(props.tournamentId),
      teamStore.fetchTeams(props.tournamentId),
      teamStore.fetchPlaceholders(props.tournamentId),
      gameStore.fetchGames(props.tournamentId),
    ])
  },
)

function openAddPhase() {
  editingPhase.value = null
  showPhaseForm.value = true
}

function openEditPhase(phase: PhaseDto) {
  editingPhase.value = phase
  showPhaseForm.value = true
}

async function handlePhaseSaved() {
  showPhaseForm.value = false
  await structureStore.fetchStructure(props.tournamentId)
  await teamStore.fetchPlaceholders(props.tournamentId)
}

async function handleDeletePhase(phaseId: string) {
  try {
    await structureStore.deletePhase(props.tournamentId, phaseId)
    showSuccess('Phase deleted')
    await teamStore.fetchPlaceholders(props.tournamentId)
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to delete phase'))
  }
}
</script>

<template>
  <div>
    <SectionHeader title="Structure">
      <v-btn v-if="isOwner" color="primary" prepend-icon="mdi-plus" @click="openAddPhase">
        Add Phase
      </v-btn>
    </SectionHeader>

    <LoadingBar :loading="structureStore.loading" class="mb-4" aria-label="Loading structure" />

    <div v-if="phases.length > 0" class="phases-list">
      <StructurePhaseCard
        v-for="phase in phases"
        :key="phase.id"
        :phase="phase"
        :tournament-id="tournamentId"
        :editing="isOwner"
        :expanded="expandedPhases.has(phase.id!)"
        :teams="teamStore.teamsWithPlaceholders"
        @toggle-phase="togglePhase(phase.id!)"
        @edit="openEditPhase"
        @delete="handleDeletePhase"
      />
    </div>

    <v-alert
      v-else-if="!structureStore.loading"
      class="mt-4 mb-4"
      type="info"
      variant="tonal"
      role="status"
    >
      No phases yet. {{ isOwner ? 'Add a phase to get started.' : '' }}
    </v-alert>

    <PhaseForm
      v-model="showPhaseForm"
      :tournament-id="tournamentId"
      :phase="editingPhase"
      :has-games="hasGamesForEditing"
      @saved="handlePhaseSaved"
    />
  </div>
</template>

<style scoped>
.phases-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
