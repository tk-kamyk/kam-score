<script setup lang="ts">
import { onMounted, ref } from 'vue'
import apiClient from '@/api/client'
import type { GameDto } from '@/game/types'

const props = defineProps<{
  tournamentId: string
  courtId: string
  courtName: string
}>()

const games = ref<GameDto[]>([])
const loading = ref(false)

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

onMounted(async () => {
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
})
</script>

<template>
  <div class="pa-3">
    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-2" />

    <v-table v-if="games.length > 0" density="compact" class="court-games-table">
      <thead>
        <tr>
          <th>Time</th>
          <th>Home</th>
          <th class="text-center">vs</th>
          <th>Away</th>
          <th>Referee</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="game in games" :key="game.id">
          <td>{{ formatTime(game.startTime) }}</td>
          <td>{{ displayTeam(game, 'home') }}</td>
          <td class="text-center">vs</td>
          <td>{{ displayTeam(game, 'away') }}</td>
          <td>{{ game.refereeTeamName ?? '-' }}</td>
        </tr>
      </tbody>
    </v-table>

    <div v-else-if="!loading" class="text-body-2 text-medium-emphasis">
      No games scheduled on this court.
    </div>
  </div>
</template>

<style scoped>
.court-games-table {
  background: transparent;
}
</style>
