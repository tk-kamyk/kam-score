<script setup lang="ts">
import { onMounted, ref, computed, watch, provide } from 'vue'
import { useGameStore } from '@/game/store'
import { useStructureStore } from '@/structure/store'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import { parseErrorDetail } from '@/api/errors'
import { useExpandedQueryParam } from '@/composables/useExpandedQueryParam'
import { useGamesByPhase } from '@/composables/useGamesByPhase'
import type { GameDto } from '@/game/types'
import type { PhaseDto } from '@/structure/types'
import SectionHeader from '@/components/SectionHeader.vue'
import SchedulePhaseCard from '@/game/SchedulePhaseCard.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const gameStore = useGameStore()
const structureStore = useStructureStore()

provide('tournamentId', props.tournamentId)
provide('isOwner', props.isOwner)
const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()
const { handleError, generalError, clearErrors } = useFormErrors()
const { expanded: expandedPhases, toggle: togglePhase } = useExpandedQueryParam('phase')
const { expanded: expandedGroups, toggle: toggleGroupKey } = useExpandedQueryParam('group')
const { phaseGames } = useGamesByPhase()

function toggleGroup(phaseId: string, groupId: string) {
  toggleGroupKey(`${phaseId}:${groupId}`)
}

const generating = ref<string | null>(null)
const completing = ref<string | null>(null)
const reopening = ref<string | null>(null)
const showDeleteDialog = ref(false)
const showCompleteDialog = ref(false)
const showReopenDialog = ref(false)
const actionPhaseId = ref<string | null>(null)
const actionPhaseName = ref('')
const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

const phases = computed(() => structureStore.structure?.phases ?? [])

async function handleGenerate(phaseId: string) {
  generating.value = phaseId
  try {
    await gameStore.generateSchedule(props.tournamentId, phaseId)
    await Promise.all([
      structureStore.fetchStructure(props.tournamentId),
      gameStore.fetchGames(props.tournamentId),
    ])
    showSuccess('Schedule generated')
  } catch (error) {
    showError(parseErrorDetail(error) ?? 'Failed to generate schedule')
  } finally {
    generating.value = null
  }
}

function confirmDelete(phase: PhaseDto) {
  actionPhaseId.value = phase.id!
  actionPhaseName.value = phase.name
  clearErrors()
  showDeleteDialog.value = true
}

async function handleDelete() {
  if (!actionPhaseId.value) return
  try {
    await gameStore.deleteGames(props.tournamentId, actionPhaseId.value)
    await Promise.all([
      gameStore.fetchGames(props.tournamentId),
      structureStore.fetchStructure(props.tournamentId),
    ])
    showDeleteDialog.value = false
    showSuccess('Games deleted')
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to delete games')
    }
  }
}

function confirmComplete(phase: PhaseDto) {
  actionPhaseId.value = phase.id!
  actionPhaseName.value = phase.name
  clearErrors()
  showCompleteDialog.value = true
}

async function handleComplete() {
  if (!actionPhaseId.value) return
  completing.value = actionPhaseId.value
  try {
    await structureStore.completePhase(props.tournamentId, actionPhaseId.value)
    await Promise.all([
      structureStore.fetchStructure(props.tournamentId),
      gameStore.fetchGames(props.tournamentId),
      teamStore.fetchPlaceholders(props.tournamentId),
    ])
    showCompleteDialog.value = false
    showSuccess('Phase completed')
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to complete phase')
    }
  } finally {
    completing.value = null
  }
}

function confirmReopen(phase: PhaseDto) {
  actionPhaseId.value = phase.id!
  actionPhaseName.value = phase.name
  clearErrors()
  showReopenDialog.value = true
}

async function handleReopen() {
  if (!actionPhaseId.value) return
  reopening.value = actionPhaseId.value
  try {
    await structureStore.reopenPhase(props.tournamentId, actionPhaseId.value)
    await Promise.all([
      structureStore.fetchStructure(props.tournamentId),
      gameStore.fetchGames(props.tournamentId),
      teamStore.fetchPlaceholders(props.tournamentId),
    ])
    showReopenDialog.value = false
    showSuccess('Phase reopened')
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to reopen phase')
    }
  } finally {
    reopening.value = null
  }
}

onMounted(async () => {
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])
})

watch(() => props.active, async (isActive) => {
  if (!isActive) return
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])
})
</script>

<template>
  <div>
    <SectionHeader title="Schedule" />

    <v-progress-linear v-if="gameStore.loading" indeterminate color="primary" class="mb-4" />

    <v-alert class="mt-4 mb-4" v-if="phases.length === 0 && !structureStore.loading" type="info" variant="tonal">
      No phases defined yet. Set up the tournament structure first.
    </v-alert>

    <div class="phases-list">
      <SchedulePhaseCard
        v-for="phase in phases"
        :key="phase.id"
        :phase="phase"
        :games="phaseGames(phase.id!)"
        :expanded="expandedPhases.has(phase.id!)"
        :expanded-groups="expandedGroups"
        :is-owner="isOwner"
        :generating="generating === phase.id"
        :completing="completing === phase.id"
        :reopening="reopening === phase.id"
        @toggle-phase="togglePhase(phase.id!)"
        @toggle-group="(groupId) => toggleGroup(phase.id!, groupId)"
        @generate="handleGenerate(phase.id!)"
        @delete="confirmDelete(phase)"
        @complete="confirmComplete(phase)"
        @reopen="confirmReopen(phase)"
        @open-result="openResultDialog"
      />
    </div>

    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Delete Games</v-card-title>
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          Are you sure you want to delete all games for "{{ actionPhaseName }}"?
          You can regenerate them afterwards.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showCompleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Complete Phase</v-card-title>
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          Complete "{{ actionPhaseName }}"? Teams will advance to the next phase based on standings.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showCompleteDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleComplete">Complete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showReopenDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Reopen Phase</v-card-title>
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          Reopen "{{ actionPhaseName }}"? This will clear team assignments and revert the next phase.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showReopenDialog = false">Cancel</v-btn>
          <v-btn color="warning" variant="elevated" @click="handleReopen">Reopen</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
