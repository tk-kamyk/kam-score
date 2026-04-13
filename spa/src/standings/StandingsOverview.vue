<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useGameStore } from '@/game/store'
import { useStructureStore } from '@/structure/store'
import { useStandingsStore } from '@/standings/store'
import { useExpandedQueryParam } from '@/composables/useExpandedQueryParam'
import { useGroupSelection } from '@/composables/useGroupSelection'
import { useGamesByPhase } from '@/composables/useGamesByPhase'
import type { GameDto } from '@/game/types'
import SectionHeader from '@/components/SectionHeader.vue'
import StandingsPhaseCard from '@/standings/StandingsPhaseCard.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const gameStore = useGameStore()
const structureStore = useStructureStore()
const standingsStore = useStandingsStore()
const {
  expanded: expandedPhases,
  toggle: togglePhaseBase,
  syncFromRoute: syncExpanded,
} = useExpandedQueryParam('phase')
const {
  selectedGroups,
  selectGroup: selectGroupBase,
  deselectGroup,
  syncFromRoute: syncGroups,
} = useGroupSelection()
const { phaseGames } = useGamesByPhase()

function togglePhase(phaseId: string) {
  togglePhaseBase(phaseId)

  if (expandedPhases.value.has(phaseId)) {
    // Fetch standings for newly expanded phase
    if (!selectedGroups.value.has(phaseId)) {
      const phase = phases.value.find((p) => p.id === phaseId)
      if (phase?.groups?.[0]?.id) {
        selectGroup(phaseId, phase.groups[0].id)
      }
    } else {
      const groupId = selectedGroups.value.get(phaseId)!
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  } else {
    deselectGroup(phaseId)
  }
}

function selectGroup(phaseId: string, groupId: string) {
  selectGroupBase(phaseId, groupId)
  standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
}

const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

watch(showResultDialog, (open) => {
  if (!open) {
    for (const [phaseId, groupId] of selectedGroups.value) {
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  }
})

watch(
  () => props.active,
  async (isActive) => {
    if (!isActive) return
    syncExpanded()
    syncGroups()
    await Promise.all([
      structureStore.fetchStructure(props.tournamentId),
      gameStore.fetchGames(props.tournamentId),
    ])
    for (const [phaseId, groupId] of selectedGroups.value) {
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  },
)

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

const phases = computed(() => structureStore.structure?.phases ?? [])

onMounted(async () => {
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])

  // Auto-select first group for expanded phases if not already selected
  for (const phaseId of expandedPhases.value) {
    if (!selectedGroups.value.has(phaseId)) {
      const phase = phases.value.find((p) => p.id === phaseId)
      if (phase?.groups?.[0]?.id) {
        selectGroup(phaseId, phase.groups[0].id)
      }
    } else {
      const groupId = selectedGroups.value.get(phaseId)!
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  }
})
</script>

<template>
  <div>
    <SectionHeader title="Groups" />

    <v-progress-linear v-if="gameStore.loading" indeterminate color="primary" class="mb-4" />

    <v-alert
      v-if="phases.length === 0 && !structureStore.loading"
      class="mt-4 mb-4"
      type="info"
      variant="tonal"
    >
      No phases defined yet. Set up the tournament structure first.
    </v-alert>

    <div class="phases-list">
      <StandingsPhaseCard
        v-for="phase in phases"
        :key="phase.id"
        :phase="phase"
        :games="phaseGames(phase.id!)"
        :standings="standingsStore.getStandings(phase.id!, selectedGroups.get(phase.id!) ?? '')"
        :expanded="expandedPhases.has(phase.id!)"
        :selected-group-id="selectedGroups.get(phase.id!) ?? null"
        @toggle-phase="togglePhase(phase.id!)"
        @select-group="(groupId) => selectGroup(phase.id!, groupId)"
        @open-result="openResultDialog"
      />
    </div>

    <GameResultDialog
      v-if="selectedGame"
      v-model="showResultDialog"
      :game="selectedGame"
      :tournament-id="tournamentId"
      :is-owner="isOwner"
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
