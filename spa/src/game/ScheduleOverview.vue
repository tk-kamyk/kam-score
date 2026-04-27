<script setup lang="ts">
import { onMounted, ref, computed, watch, provide } from 'vue'
import { useGameStore } from '@/game/store'
import { useStructureStore } from '@/structure/store'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { parseErrorDetail } from '@/api/errors'
import { useExpandedQueryParam } from '@/composables/useExpandedQueryParam'
import { useGroupSelection } from '@/composables/useGroupSelection'
import { useGamesByPhase } from '@/composables/useGamesByPhase'
import type { GameDto } from '@/game/types'
import type { PhaseDto } from '@/structure/types'
import SectionHeader from '@/components/SectionHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import SchedulePhaseCard from '@/game/SchedulePhaseCard.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const gameStore = useGameStore()
const structureStore = useStructureStore()
const teamStore = useTeamStore()

provide('tournamentId', props.tournamentId)
provide(
  'isOwner',
  computed(() => props.isOwner),
)

const { showSuccess, showError } = useSnackbar()
const {
  expanded: expandedPhases,
  toggle: togglePhaseBase,
  syncFromRoute: syncExpanded,
} = useExpandedQueryParam('phase')
const {
  selectedGroups,
  selectGroup,
  deselectGroup,
  syncFromRoute: syncGroups,
} = useGroupSelection()
const { phaseGames } = useGamesByPhase()

const phases = computed(() => structureStore.structure?.phases ?? [])

function togglePhase(phaseId: string) {
  togglePhaseBase(phaseId)

  if (expandedPhases.value.has(phaseId)) {
    if (!selectedGroups.value.has(phaseId)) {
      const phase = phases.value.find((p) => p.id === phaseId)
      if (phase?.groups?.[0]?.id) {
        selectGroup(phaseId, phase.groups[0].id)
      }
    }
  } else {
    deselectGroup(phaseId)
  }
}

// --- Per-phase action state ---

const generating = ref<string | null>(null)
const completing = ref<string | null>(null)
const reopening = ref<string | null>(null)

type PhaseAction = 'delete' | 'complete' | 'reopen'
const pendingAction = ref<PhaseAction | null>(null)
const actionPhaseId = ref<string | null>(null)
const actionPhaseName = ref('')
const actionDialog = ref<InstanceType<typeof ConfirmDialog> | null>(null)

function requestAction(action: PhaseAction, phase: PhaseDto) {
  actionPhaseId.value = phase.id!
  actionPhaseName.value = phase.name
  pendingAction.value = action
}

const actionPhaseIsCustom = computed(() => {
  if (!actionPhaseId.value) return false
  return phases.value.find((p) => p.id === actionPhaseId.value)?.format === 'Custom'
})

const actionConfig = computed(() => {
  const deleteConfig = actionPhaseIsCustom.value
    ? {
        title: 'Reset Phase',
        message: `Reset "${actionPhaseName.value}"? Manual standings will be cleared and the phase will return to a state where teams can be edited.`,
        label: 'Reset',
        color: 'error',
      }
    : {
        title: 'Delete Games',
        message: `Are you sure you want to delete all games for "${actionPhaseName.value}"? You can regenerate them afterwards.`,
        label: 'Delete',
        color: 'error',
      }

  const configs: Record<
    PhaseAction,
    { title: string; message: string; label: string; color: string }
  > = {
    delete: deleteConfig,
    complete: {
      title: 'Complete Phase',
      message: `Complete "${actionPhaseName.value}"? Teams will advance to the next phase based on standings.`,
      label: 'Complete',
      color: 'primary',
    },
    reopen: {
      title: 'Reopen Phase',
      message: `Reopen "${actionPhaseName.value}"? This will clear team assignments and revert the next phase.`,
      label: 'Reopen',
      color: 'warning',
    },
  }
  return pendingAction.value ? configs[pendingAction.value] : null
})

const showActionDialog = computed({
  get: () => pendingAction.value !== null,
  set: (open: boolean) => {
    if (!open) pendingAction.value = null
  },
})

async function runAction() {
  if (!pendingAction.value || !actionPhaseId.value) return
  const action = pendingAction.value
  const phaseId = actionPhaseId.value

  const handlers: Record<PhaseAction, () => Promise<void>> = {
    delete: async () => {
      const wasCustom = actionPhaseIsCustom.value
      await gameStore.deleteGames(props.tournamentId, phaseId)
      await Promise.all([
        gameStore.fetchGames(props.tournamentId),
        structureStore.fetchStructure(props.tournamentId),
      ])
      showSuccess(wasCustom ? 'Phase reset' : 'Games deleted')
    },
    complete: async () => {
      completing.value = phaseId
      try {
        await structureStore.completePhase(props.tournamentId, phaseId)
        await Promise.all([
          structureStore.fetchStructure(props.tournamentId),
          gameStore.fetchGames(props.tournamentId),
          teamStore.fetchPlaceholders(props.tournamentId),
        ])
        showSuccess('Phase completed')
      } finally {
        completing.value = null
      }
    },
    reopen: async () => {
      reopening.value = phaseId
      try {
        await structureStore.reopenPhase(props.tournamentId, phaseId)
        await Promise.all([
          structureStore.fetchStructure(props.tournamentId),
          gameStore.fetchGames(props.tournamentId),
          teamStore.fetchPlaceholders(props.tournamentId),
        ])
        showSuccess('Phase reopened')
      } finally {
        reopening.value = null
      }
    },
  }

  try {
    await handlers[action]()
    pendingAction.value = null
  } catch (error) {
    if (!actionDialog.value?.handleError(error)) {
      showError(`Failed to ${action} phase`)
    }
  }
}

async function handleGenerate(phaseId: string) {
  generating.value = phaseId
  const phase = phases.value.find((p) => p.id === phaseId)
  const successMessage = phase?.format === 'Custom' ? 'Phase started' : 'Schedule generated'
  try {
    await gameStore.generateSchedule(props.tournamentId, phaseId)
    await Promise.all([
      structureStore.fetchStructure(props.tournamentId),
      gameStore.fetchGames(props.tournamentId),
    ])
    showSuccess(successMessage)
  } catch (error) {
    showError(parseErrorDetail(error) ?? 'Failed to generate schedule')
  } finally {
    generating.value = null
  }
}

// --- Result dialog ---

const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

onMounted(async () => {
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])

  for (const phaseId of expandedPhases.value) {
    if (!selectedGroups.value.has(phaseId)) {
      const phase = phases.value.find((p) => p.id === phaseId)
      if (phase?.groups?.[0]?.id) {
        selectGroup(phaseId, phase.groups[0].id)
      }
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
  },
)
</script>

<template>
  <div>
    <SectionHeader title="Schedule" />

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
      <SchedulePhaseCard
        v-for="phase in phases"
        :key="phase.id"
        :phase="phase"
        :games="phaseGames(phase.id!)"
        :expanded="expandedPhases.has(phase.id!)"
        :selected-group-id="selectedGroups.get(phase.id!) ?? null"
        :is-owner="isOwner"
        :generating="generating === phase.id"
        :completing="completing === phase.id"
        :reopening="reopening === phase.id"
        @toggle-phase="togglePhase(phase.id!)"
        @select-group="(groupId) => selectGroup(phase.id!, groupId)"
        @generate="handleGenerate(phase.id!)"
        @delete="requestAction('delete', phase)"
        @complete="requestAction('complete', phase)"
        @reopen="requestAction('reopen', phase)"
        @open-result="openResultDialog"
      />
    </div>

    <ConfirmDialog
      v-if="actionConfig"
      ref="actionDialog"
      v-model="showActionDialog"
      :title="actionConfig.title"
      :message="actionConfig.message"
      :confirm-label="actionConfig.label"
      :confirm-color="actionConfig.color"
      @confirm="runAction"
    />

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
