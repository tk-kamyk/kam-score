<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import apiClient from '@/api/client'
import type { GameDto } from '@/game/types'
import { useRefereeDialog } from '@/composables/useRefereeDialog'
import GameResultDisplay from '@/game/GameResultDisplay.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'
import RefereeAssignDialog from '@/game/RefereeAssignDialog.vue'

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
const { showRefereeDialog, refereeGame, openRefereeDialog } = useRefereeDialog()

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

const hasLabels = computed(() => games.value.some(g => g.label))

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
          <th v-if="hasLabels">Game</th>
          <th>Time</th>
          <th>Home</th>
          <th class="text-center">Result</th>
          <th>Away</th>
          <th>Referee</th>
          <th aria-label="Actions" />
        </tr>
      </thead>
      <tbody>
        <tr v-for="game in games" :key="game.id">
          <td v-if="hasLabels">{{ game.label ?? '-' }}</td>
          <td>{{ formatTime(game.startTime) }}</td>
          <td>{{ displayTeam(game, 'home') }}</td>
          <td class="text-center">
            <GameResultDisplay :game="game" @enter-result="openResultDialog(game)" />
          </td>
          <td>{{ displayTeam(game, 'away') }}</td>
          <td>
              <v-btn
                v-if="isOwner && game.refereeTeamName"
                size="small"
                variant="text"
                aria-label="Reassign referee"
                @click="openRefereeDialog(game)"
              >
                {{ game.refereeTeamName }}
              </v-btn>
              <v-btn
                v-else-if="isOwner"
                size="small"
                variant="text"
                icon="mdi-whistle"
                aria-label="Assign referee"
                @click="openRefereeDialog(game)"
              />
              <template v-else-if="game.refereeTeamName">{{ game.refereeTeamName }}</template>
              <template v-else>-</template>
            </td>
          <td class="text-right">
            <v-btn
              v-if="game.status === 'Completed' && game.homeScore != null"
              size="small"
              variant="text"
              icon="mdi-pencil"
              :aria-label="'Edit result for ' + displayTeam(game, 'home') + ' vs ' + displayTeam(game, 'away')"
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

    <RefereeAssignDialog
      v-if="refereeGame"
      v-model="showRefereeDialog"
      :game="refereeGame"
      :tournament-id="tournamentId"
      @assigned="loadGames"
    />
  </div>
</template>

<style scoped>
.court-games-table {
  background: transparent;
}
</style>
