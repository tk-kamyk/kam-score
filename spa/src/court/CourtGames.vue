<script setup lang="ts">
import { onMounted, ref } from 'vue'
import apiClient from '@/api/client'
import type { GameDto } from '@/game/types'
import GameResultDialog from '@/game/GameResultDialog.vue'

const props = defineProps<{
  tournamentId: string
  courtId: string
  courtName: string
  isOwner: boolean
}>()

const games = ref<GameDto[]>([])
const loading = ref(false)
const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

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
  return game.sets?.map(s => `${s.homePoints}–${s.awayPoints}`).join(' / ') || ''
}

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

async function loadGames() {
  loading.value = true
  try {
    const params = new URLSearchParams({ courtId: props.courtId })
    const { data } = await apiClient.get<GameDto[]>(
      `/tournaments/${props.tournamentId}/games?${params}`,
    )
    games.value = data
  } finally {
    loading.value = false
  }
}

function onResultDialogClose(open: boolean) {
  if (!open) loadGames()
}

onMounted(loadGames)
</script>

<template>
  <div class="pa-3">
    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-2" />

    <v-table v-if="games.length > 0" density="compact" class="court-games-table">
      <thead>
        <tr>
          <th>Time</th>
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
          <td>{{ displayTeam(game, 'home') }}</td>
          <td class="text-center">
            <template v-if="game.status === 'Completed' && game.homeScore != null">
              <v-chip size="small" color="success" variant="tonal">
                <template v-if="!game.sets?.length || (game.sets?.length ?? 0) > 1">{{ game.homeScore }}–{{ game.awayScore }}</template>
                <template v-else>{{ game.sets?.[0]?.homePoints }}–{{ game.sets?.[0]?.awayPoints }}</template>
              </v-chip>
              <div v-if="(game.sets?.length ?? 0) > 1" class="text-body-small text-medium-emphasis mt-1">
                {{ formatSets(game) }}
              </div>
            </template>
            <span v-else class="text-medium-emphasis">vs</span>
          </td>
          <td>{{ displayTeam(game, 'away') }}</td>
          <td>{{ game.refereeTeamName ?? '-' }}</td>
          <td class="text-right">
            <v-btn
              size="small"
              color="primary"
              variant="tonal"
              prepend-icon="mdi-scoreboard"
              append-icon="mdi-pencil"
              @click="openResultDialog(game)"
            />
          </td>
        </tr>
      </tbody>
    </v-table>

    <div v-else-if="!loading" class="text-body-medium text-medium-emphasis">
      No games scheduled on this court.
    </div>

    <GameResultDialog
      v-if="selectedGame"
      v-model="showResultDialog"
      :game="selectedGame"
      :tournament-id="tournamentId"
      :is-owner="isOwner"
      @update:model-value="onResultDialogClose"
    />
  </div>
</template>

<style scoped>
.court-games-table {
  background: transparent;
}
</style>
