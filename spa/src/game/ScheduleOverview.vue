<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGameStore } from '@/game/store'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import type { GameDto } from '@/game/types'
import type { PhaseDto } from '@/structure/types'
import SchedulePhaseCard from '@/game/SchedulePhaseCard.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
}>()

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()

function parseQuerySet(param: unknown): Set<string> {
  if (!param || typeof param !== 'string') return new Set()
  return new Set(param.split(',').filter(Boolean))
}

const expandedPhases = ref(parseQuerySet(route.query.phase))
const expandedGroups = ref(parseQuerySet(route.query.group))

watch(expandedPhases, (phases) => {
  const query = { ...route.query }
  if (phases.size > 0) {
    query.phase = [...phases].join(',')
  } else {
    delete query.phase
  }
  router.replace({ query })
})

watch(expandedGroups, (groups) => {
  const query = { ...route.query }
  if (groups.size > 0) {
    query.group = [...groups].join(',')
  } else {
    delete query.group
  }
  router.replace({ query })
})

function togglePhase(phaseId: string) {
  if (expandedPhases.value.has(phaseId)) {
    expandedPhases.value.delete(phaseId)
  } else {
    expandedPhases.value.add(phaseId)
  }
  expandedPhases.value = new Set(expandedPhases.value)
}

function toggleGroup(phaseId: string, groupId: string) {
  const key = `${phaseId}:${groupId}`
  if (expandedGroups.value.has(key)) {
    expandedGroups.value.delete(key)
  } else {
    expandedGroups.value.add(key)
  }
  expandedGroups.value = new Set(expandedGroups.value)
}

const generating = ref<string | null>(null)
const showDeleteDialog = ref(false)
const deletingPhaseId = ref<string | null>(null)
const deletingPhaseName = ref('')
const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

const phases = computed(() => structureStore.structure?.phases ?? [])

const gamesByPhase = computed(() => {
  const map: Record<string, GameDto[]> = {}
  for (const game of gameStore.games) {
    const key = game.phaseId ?? ''
    if (!map[key]) map[key] = []
    map[key].push(game)
  }
  return map
})

function phaseGames(phaseId: string): GameDto[] {
  return gamesByPhase.value[phaseId] ?? []
}

async function handleGenerate(phaseId: string) {
  generating.value = phaseId
  try {
    await gameStore.generateSchedule(props.tournamentId, phaseId)
    await gameStore.fetchGames(props.tournamentId)
    showSuccess('Schedule generated')
  } catch {
    showError('Failed to generate schedule')
  } finally {
    generating.value = null
  }
}

function confirmDelete(phase: PhaseDto) {
  deletingPhaseId.value = phase.id!
  deletingPhaseName.value = phase.name
  showDeleteDialog.value = true
}

async function handleDelete() {
  if (!deletingPhaseId.value) return
  try {
    await gameStore.deleteGames(props.tournamentId, deletingPhaseId.value)
    await gameStore.fetchGames(props.tournamentId)
    showDeleteDialog.value = false
    showSuccess('Games deleted')
  } catch {
    showError('Failed to delete games')
  }
}

onMounted(async () => {
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])
})
</script>

<template>
  <div>
    <h3 class="section-title text-title-small text-md-title-medium mb-6">Schedule</h3>

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
        @toggle-phase="togglePhase(phase.id!)"
        @toggle-group="(groupId) => toggleGroup(phase.id!, groupId)"
        @generate="handleGenerate(phase.id!)"
        @delete="confirmDelete(phase)"
        @open-result="openResultDialog"
      />
    </div>

    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Delete Games</v-card-title>
        <v-card-text>
          Are you sure you want to delete all games for "{{ deletingPhaseName }}"?
          You can regenerate them afterwards.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
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
