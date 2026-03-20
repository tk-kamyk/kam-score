<script setup lang="ts">
import { computed, inject } from 'vue'
import type { GameDto } from '@/game/types'
import { useGameStore } from '@/game/store'
import { useRefereeDialog } from '@/composables/useRefereeDialog'
import GameResultDisplay from '@/game/GameResultDisplay.vue'
import RefereeAssignDialog from '@/game/RefereeAssignDialog.vue'

const props = defineProps<{
  phaseId: string
  groupId: string
  groupName: string
  games: GameDto[]
  expanded: boolean
  singleGroup?: boolean
}>()

const tournamentId = inject<string>('tournamentId')!
const isOwner = inject<boolean>('isOwner')!
const gameStore = useGameStore()
const { showRefereeDialog, refereeGame, openRefereeDialog } = useRefereeDialog()

const hasLabels = computed(() => props.games.some(g => g.label))

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
    <div v-if="!singleGroup" class="d-flex align-center text-title-medium font-weight-medium group-header" @click.stop="emit('toggle')">
      <v-icon
        :icon="expanded ? 'mdi-chevron-down' : 'mdi-chevron-right'"
        size="small"
        class="mr-1"
      />
      Group {{ groupName }}
    </div>
    <v-card v-if="singleGroup || expanded" class="my-6 data-table-card">
      <v-table density="compact" class="styled-table">
        <thead>
          <tr>
            <th v-if="hasLabels">Game</th>
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
            <td v-if="hasLabels">{{ game.label ?? '-' }}</td>
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
            <td>
              <template v-if="game.refereeTeamName">{{ game.refereeTeamName }}</template>
              <v-btn
                v-else-if="isOwner"
                size="small"
                variant="text"
                icon="mdi-whistle"
                aria-label="Assign referee"
                @click="openRefereeDialog(game)"
              />
              <template v-else>-</template>
            </td>
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
    <RefereeAssignDialog
      v-if="refereeGame"
      v-model="showRefereeDialog"
      :game="refereeGame"
      :tournament-id="tournamentId"
      @assigned="gameStore.fetchGames(tournamentId)"
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
  background-color: var(--ks-border-subtle);
}

.group-header {
  cursor: pointer;
}
</style>
