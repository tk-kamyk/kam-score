<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGameStore } from '@/game/store'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { formatPhaseFormat } from '@/structure/types'
import type { GameDto } from '@/game/types'
import type { PhaseDto } from '@/structure/types'
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

function gamesByGroup(games: GameDto[]): Record<string, GameDto[]> {
  const map: Record<string, GameDto[]> = {}
  for (const game of games) {
    const key = game.groupId ?? ''
    if (!map[key]) map[key] = []
    map[key].push(game)
  }
  return map
}

function groupName(phase: PhaseDto, groupId: string): string {
  const group = phase.groups?.find(g => g.id === groupId)
  return group?.name ?? groupId
}

function formatTime(startTime?: string): string {
  if (!startTime) return '-'
  const date = new Date(startTime)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function displayTeam(game: GameDto, side: 'home' | 'away'): string {
  const name = side === 'home' ? game.homeTeamName : game.awayTeamName
  if (name) return name
  const placeholder = side === 'home' ? game.homeTeamPlaceholder : game.awayTeamPlaceholder
  return placeholder ?? '-'
}

function formatSets(game: GameDto): string {
  if (!game.sets?.length) return ''
  return game.sets.map(s => `${s.homePoints}–${s.awayPoints}`).join(' / ')
}

function isPlaceholder(game: GameDto, side: 'home' | 'away'): boolean {
  const name = side === 'home' ? game.homeTeamName : game.awayTeamName
  if (name) return false
  const placeholder = side === 'home' ? game.homeTeamPlaceholder : game.awayTeamPlaceholder
  return !!placeholder
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
    <h3 class="section-title mb-6">Schedule</h3>

    <v-progress-linear v-if="gameStore.loading" indeterminate color="primary" class="mb-4" />

    <v-alert class="mt-4 mb-4" v-if="phases.length === 0 && !structureStore.loading" type="info" variant="tonal">
      No phases defined yet. Set up the tournament structure first.
    </v-alert>

    <div class="phases-list">
    <v-card v-for="phase in phases" :key="phase.id" class="phase-card">
      <v-card-title class="d-flex align-center justify-space-between phase-header mb-4 mt-4" @click="togglePhase(phase.id!)">
        <div class="d-flex align-center">
          <v-icon
            :icon="expandedPhases.has(phase.id!) ? 'mdi-chevron-down' : 'mdi-chevron-right'"
            size="small"
            class="mr-1"
          />
          <span class="text-h6">{{ phase.name }}</span>
          <v-chip size="small" class="ml-2" color="primary" variant="tonal">
            {{ formatPhaseFormat(phase.format) }}
          </v-chip>
          <v-chip v-if="phase.startTime" size="small" class="ml-2" color="warning" variant="tonal">
            Starts {{ phase.startTime }}
          </v-chip>
        </div>
        <div v-if="isOwner" @click.stop>
          <v-btn
            v-if="phaseGames(phase.id!).length === 0"
            color="primary"
            prepend-icon="mdi-calendar-clock"
            :loading="generating === phase.id"
            @click="handleGenerate(phase.id!)"
          >
            Generate &amp; Schedule
          </v-btn>
          <v-btn
            v-else
            color="error"
            variant="tonal"
            prepend-icon="mdi-delete"
            @click="confirmDelete(phase)"
          >
            Delete Games
          </v-btn>
        </div>
      </v-card-title>

      <v-card-text v-if="expandedPhases.has(phase.id!)">
      <template v-if="phaseGames(phase.id!).length > 0">
        <div
          v-for="(games, groupId) in gamesByGroup(phaseGames(phase.id!))"
          :key="groupId"
          class="mb-4 ml-6 mr-6"
        >
          <div class="d-flex align-center text-subtitle-1 font-weight-medium mb-6 mt-6 group-header" @click.stop="toggleGroup(phase.id!, groupId as string)">
            <v-icon
              :icon="expandedGroups.has(`${phase.id}:${groupId}`) ? 'mdi-chevron-down' : 'mdi-chevron-right'"
              size="small"
              class="mr-1"
            />
            Group {{ groupName(phase, groupId as string) }}
          </div>
          <v-card v-if="expandedGroups.has(`${phase.id}:${groupId}`)" class="data-table-card">
            <v-table density="comfortable" class="styled-table">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Court</th>
                  <th>Home</th>
                  <th class="text-center">Result</th>
                  <th>Away</th>
                  <th>Referee</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                <tr v-for="game in games" :key="game.id">
                  <td>{{ formatTime(game.startTime) }}</td>
                  <td>{{ game.courtName ?? '-' }}</td>
                  <td :class="{ 'text-italic text-medium-emphasis': isPlaceholder(game, 'home') }">
                    {{ displayTeam(game, 'home') }}
                  </td>
                  <td class="text-center pt-2 pb-2">
                    <template v-if="game.status === 'Completed' && game.homeScore != null">
                      <v-chip size="small" color="success" variant="tonal">
                        {{ game.homeScore }}–{{ game.awayScore }}
                      </v-chip>
                      <div v-if="formatSets(game)" class="text-caption text-medium-emphasis mt-1">
                        {{ formatSets(game) }}
                      </div>
                    </template>
                    <span v-else class="text-medium-emphasis">vs</span>
                  </td>
                  <td :class="{ 'text-italic text-medium-emphasis': isPlaceholder(game, 'away') }">
                    {{ displayTeam(game, 'away') }}
                  </td>
                  <td>{{ game.refereeTeamName ?? '-' }}</td>
                  <td class="text-right">
                    <v-btn
                      v-if="game.status === 'Scheduled'"
                      size="x-small"
                      variant="tonal"
                      color="primary"
                      @click="openResultDialog(game)"
                    >
                      Enter Result
                    </v-btn>
                    <v-btn
                      v-else-if="game.status === 'Completed'"
                      size="x-small"
                      variant="text"
                      icon="mdi-pencil"
                      @click="openResultDialog(game)"
                    />
                  </td>
                </tr>
              </tbody>
            </v-table>
          </v-card>
        </div>
      </template>

      <v-alert
        v-else-if="!gameStore.loading"
        class="mt-4 mb-4"
        type="info"
        variant="tonal"
        density="compact"
      >
        No games generated for this phase yet.
      </v-alert>
      </v-card-text>
    </v-card>
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
.data-table-card {
  border: 1px solid var(--ks-border);
}

.styled-table thead tr {
  background-color: rgb(var(--v-theme-surface-bright));
}

.styled-table tbody tr:hover {
  background-color: var(--ks-border-subtle) !important;
}

.phases-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.phase-card {
  border: 1px solid var(--ks-border);
}

.phase-header {
  cursor: pointer;
}

.group-header {
  cursor: pointer;
}
</style>
