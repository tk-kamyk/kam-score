<script setup lang="ts">
import { computed } from 'vue'
import type { GameDto } from '@/game/types'
import GameResultDisplay from '@/game/GameResultDisplay.vue'

const props = defineProps<{
  games: GameDto[]
}>()

const hasLabels = computed(() => props.games.some((g) => g.label))

const emit = defineEmits<{
  'open-result': [game: GameDto]
}>()

function displayTeam(game: GameDto, side: 'home' | 'away'): string {
  const name = side === 'home' ? game.homeTeamName : game.awayTeamName
  if (name) return name
  const placeholder = side === 'home' ? game.homeTeamPlaceholder : game.awayTeamPlaceholder
  return placeholder ?? '-'
}

function isPlaceholder(game: GameDto, side: 'home' | 'away'): boolean {
  const name = side === 'home' ? game.homeTeamName : game.awayTeamName
  if (name) return false
  const placeholder = side === 'home' ? game.homeTeamPlaceholder : game.awayTeamPlaceholder
  return !!placeholder
}
</script>

<template>
  <v-card class="data-table-card">
    <v-table density="compact" class="styled-table">
      <thead>
        <tr>
          <th v-if="hasLabels">Game</th>
          <th>Round</th>
          <th>Home</th>
          <th class="text-center">Result</th>
          <th>Away</th>
          <th aria-label="Actions" />
        </tr>
      </thead>
      <tbody>
        <tr v-for="game in games" :key="game.id">
          <td v-if="hasLabels">{{ game.label ?? '-' }}</td>
          <td>{{ game.round }}</td>
          <td :class="{ 'text-italic text-medium-emphasis': isPlaceholder(game, 'home') }">
            {{ displayTeam(game, 'home') }}
          </td>
          <td class="text-center">
            <GameResultDisplay :game="game" @enter-result="emit('open-result', game)" />
          </td>
          <td :class="{ 'text-italic text-medium-emphasis': isPlaceholder(game, 'away') }">
            {{ displayTeam(game, 'away') }}
          </td>
          <td class="text-right">
            <v-btn
              v-if="game.status === 'Completed' && game.homeScore != null"
              size="small"
              variant="text"
              icon="mdi-pencil"
              :aria-label="
                'Edit result for ' + displayTeam(game, 'home') + ' vs ' + displayTeam(game, 'away')
              "
              @click="emit('open-result', game)"
            />
          </td>
        </tr>
      </tbody>
    </v-table>
  </v-card>
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
</style>
