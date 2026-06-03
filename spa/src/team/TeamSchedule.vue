<script setup lang="ts">
import { ref, computed } from 'vue'
import type { GameDto } from '@/game/types'
import { useGameStore } from '@/game/store'
import { useGamesByPhase } from '@/composables/useGamesByPhase'
import GameResultDisplay from '@/game/GameResultDisplay.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'
import LoadingBar from '@/components/LoadingBar.vue'

const props = defineProps<{
  tournamentId: string
  teamId: string
  teamName: string
  isOwner: boolean
}>()

const gameStore = useGameStore()
const { teamGames } = useGamesByPhase()

const loading = computed(() => gameStore.loading)
const showBreaks = ref(true)
const games = computed(() => teamGames(props.teamId))
const allTimeSlots = computed(
  () => new Set(gameStore.games.map((g) => g.startTime).filter(Boolean) as string[]),
)
const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

interface PhaseGroup {
  phaseId: string
  phaseName: string
  levelName?: string
  groupId: string
  groupName: string
  games: GameDto[]
}

interface ScheduleEntry {
  type: 'game' | 'break'
  time: string
  game?: GameDto
  role?: 'Home' | 'Away' | 'Referee'
}

const teamTimeSlots = computed(() => {
  return new Set(games.value.map((g) => g.startTime).filter(Boolean))
})

const groupedGames = computed<PhaseGroup[]>(() => {
  const map = new Map<string, PhaseGroup>()
  for (const game of games.value) {
    const key = `${game.phaseId}:${game.groupId}`
    if (!map.has(key)) {
      map.set(key, {
        phaseId: game.phaseId!,
        phaseName: game.phaseName ?? game.phaseId!,
        levelName: game.levelName,
        groupId: game.groupId!,
        groupName: game.groupName ?? game.groupId!,
        games: [],
      })
    }
    map.get(key)!.games.push(game)
  }
  return [...map.values()]
})

const entriesByGroup = computed<Map<string, ScheduleEntry[]>>(() => {
  const result = new Map<string, ScheduleEntry[]>()
  for (const group of groupedGames.value) {
    const key = `${group.phaseId}:${group.groupId}`
    const timedEntries: ScheduleEntry[] = []
    const untimedEntries: ScheduleEntry[] = []
    for (const game of group.games) {
      const entry: ScheduleEntry = {
        type: 'game',
        time: game.startTime ?? '',
        game,
        role: teamRole(game),
      }
      if (game.startTime != null) timedEntries.push(entry)
      else untimedEntries.push(entry)
    }
    timedEntries.sort((a, b) => a.time.localeCompare(b.time))

    if (!showBreaks.value || timedEntries.length === 0) {
      result.set(key, [...timedEntries, ...untimedEntries])
      continue
    }

    // Find break slots that fall between this group's first and last timed game.
    const firstTime = timedEntries[0].time
    const lastTime = timedEntries[timedEntries.length - 1].time

    const breakEntries: ScheduleEntry[] = [...allTimeSlots.value]
      .filter((slot) => slot >= firstTime && slot <= lastTime && !teamTimeSlots.value.has(slot))
      .map((slot) => ({ type: 'break' as const, time: slot }))

    const interleaved = [...timedEntries, ...breakEntries].sort((a, b) =>
      a.time.localeCompare(b.time),
    )
    result.set(key, [...interleaved, ...untimedEntries])
  }
  return result
})

function teamRole(game: GameDto): 'Home' | 'Away' | 'Referee' {
  if (game.homeTeamId === props.teamId) return 'Home'
  if (game.awayTeamId === props.teamId) return 'Away'
  return 'Referee'
}

function roleColor(role: 'Home' | 'Away' | 'Referee'): string {
  if (role === 'Referee') return 'warning'
  return 'primary'
}

function opponentName(game: GameDto): string {
  const role = teamRole(game)
  if (role === 'Home') return game.awayTeamName ?? game.awayTeamPlaceholder ?? '-'
  if (role === 'Away') return game.homeTeamName ?? game.homeTeamPlaceholder ?? '-'
  return `${game.homeTeamName ?? '?'} vs ${game.awayTeamName ?? '?'}`
}

function formatTime(startTime?: string): string {
  if (!startTime) return '-'
  const date = new Date(startTime)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function groupLabel(group: PhaseGroup): string {
  const { levelName, groupName } = group
  if (!levelName) return `Group ${groupName}`
  if (levelName === groupName) return levelName
  return `${levelName} - Group ${groupName}`
}
</script>

<template>
  <div class="pa-3">
    <LoadingBar :loading="loading" class="mb-2" />

    <template v-if="games.length > 0">
      <div class="d-flex align-center justify-end mb-2">
        <v-switch
          v-model="showBreaks"
          label="Show breaks"
          density="compact"
          hide-details
          color="primary"
          class="show-breaks-toggle"
        />
      </div>

      <div class="mb-5">
        <v-table density="compact" class="team-schedule-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Court</th>
              <th>Opponent</th>
              <th class="text-center">Result</th>
              <th>Role</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="group in groupedGames" :key="`${group.phaseId}:${group.groupId}`">
              <tr class="phase-group-header">
                <td colspan="5" class="text-center">
                  <div>
                    <strong>{{ group.phaseName }}</strong>
                  </div>
                  <div>{{ groupLabel(group) }}</div>
                </td>
              </tr>
              <template
                v-for="entry in entriesByGroup.get(`${group.phaseId}:${group.groupId}`) ?? []"
                :key="entry.type === 'game' ? entry.game!.id : `break-${entry.time}`"
              >
                <tr v-if="entry.type === 'game'">
                  <td>{{ formatTime(entry.game!.startTime) }}</td>
                  <td>{{ entry.game!.courtName ?? '-' }}</td>
                  <td>{{ opponentName(entry.game!) }}</td>
                  <td class="text-center">
                    <GameResultDisplay
                      :game="entry.game!"
                      @enter-result="openResultDialog(entry.game!)"
                    />
                  </td>
                  <td>
                    <v-chip size="small" :color="roleColor(entry.role!)" variant="tonal">
                      {{ entry.role }}
                    </v-chip>
                  </td>
                </tr>
                <tr v-else class="break-row">
                  <td>{{ formatTime(entry.time) }}</td>
                  <td colspan="4" class="text-medium-emphasis text-italic">Break</td>
                </tr>
              </template>
            </template>
          </tbody>
        </v-table>
      </div>
    </template>

    <div v-else-if="!loading" class="text-body-medium text-medium-emphasis">
      No games scheduled for this team.
    </div>

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
.team-schedule-table {
  background: transparent;
}

.show-breaks-toggle {
  flex: none;
}

.break-row {
  opacity: 0.6;
}

.phase-group-header td {
  background: rgba(var(--v-theme-on-surface), 0.04);
}

.phase-group-header:not(:first-child) td {
  border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}
</style>
