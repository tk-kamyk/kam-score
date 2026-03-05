<script setup lang="ts">
import type { GameDto } from '@/game/types'
import GameResultDisplay from '@/game/GameResultDisplay.vue'

defineProps<{
  phaseId: string
  groupId: string
  groupName: string
  games: GameDto[]
  expanded: boolean
}>()

const emit = defineEmits<{
  toggle: []
  'open-result': [game: GameDto]
}>()

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

function isPlaceholder(game: GameDto, side: 'home' | 'away'): boolean {
  if (side === 'home') {
    if (game.homeTeamIsPlaceholder) return true
    return !game.homeTeamName && !!game.homeTeamPlaceholder
  }
  if (game.awayTeamIsPlaceholder) return true
  return !game.awayTeamName && !!game.awayTeamPlaceholder
}
</script>

<template>
  <div class="py-2 mx-0 mx-sm-6">
    <div class="d-flex align-center text-title-medium font-weight-medium group-header" @click.stop="emit('toggle')">
      <v-icon
        :icon="expanded ? 'mdi-chevron-down' : 'mdi-chevron-right'"
        size="small"
        class="mr-1"
      />
      Group {{ groupName }}
    </div>
    <v-card v-if="expanded" class="my-6 data-table-card">
      <v-table density="compact" class="styled-table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Court</th>
            <th>Home</th>
            <th class="text-center">Result</th>
            <th>Away</th>
            <th>Referee</th>
            <th aria-label="Actions" />
          </tr>
        </thead>
        <tbody>
          <tr v-for="game in games" :key="game.id">
            <td>{{ formatTime(game.startTime) }}</td>
            <td>{{ game.courtName ?? '-' }}</td>
            <td :class="{ 'text-italic text-medium-emphasis': isPlaceholder(game, 'home') }">
              {{ displayTeam(game, 'home') }}
            </td>
            <td class="text-center">
              <GameResultDisplay :game="game" @enter-result="emit('open-result', game)" />
            </td>
            <td :class="{ 'text-italic text-medium-emphasis': isPlaceholder(game, 'away') }">
              {{ displayTeam(game, 'away') }}
            </td>
            <td>{{ game.refereeTeamName ?? '-' }}</td>
            <td class="text-right">
              <v-btn
                v-if="game.status === 'Completed' && game.homeScore != null"
                size="small"
                variant="text"
                icon="mdi-pencil"
                :aria-label="'Edit result for ' + displayTeam(game, 'home') + ' vs ' + displayTeam(game, 'away')"
                @click="emit('open-result', game)"
              />
            </td>
          </tr>
        </tbody>
      </v-table>
    </v-card>
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
  background-color: var(--ks-border-subtle);
}

.group-header {
  cursor: pointer;
}
</style>
