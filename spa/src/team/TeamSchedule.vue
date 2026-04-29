<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import apiClient from '@/api/client'
import type { GameDto } from '@/game/types'
import GameResultDisplay from '@/game/GameResultDisplay.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'
import LoadingBar from '@/components/LoadingBar.vue'

const props = defineProps<{
  tournamentId: string
  teamId: string
  teamName: string
  isOwner: boolean
}>()

const loading = ref(false)
const showBreaks = ref(false)
const games = ref<GameDto[]>([])
const allTimeSlots = ref<Set<string>>(new Set())
const timeSlotsLoaded = ref(false)
const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

async function loadGames() {
  loading.value = true
  try {
    const response = await apiClient.get<GameDto[]>(
      `/tournaments/${props.tournamentId}/games?teamId=${props.teamId}`,
    )
    games.value = response.data
  } finally {
    loading.value = false
  }
}

async function loadAllTimeSlots() {
  const response = await apiClient.get<GameDto[]>(`/tournaments/${props.tournamentId}/games`)
  allTimeSlots.value = new Set(response.data.map((g) => g.startTime).filter(Boolean) as string[])
  timeSlotsLoaded.value = true
}

watch(showBreaks, (on) => {
  if (on && !timeSlotsLoaded.value) loadAllTimeSlots()
})

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

async function onResultSaved() {
  await loadGames()
  if (timeSlotsLoaded.value) await loadAllTimeSlots()
}

onMounted(loadGames)

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

function phaseGroupEntries(phaseGroup: PhaseGroup): ScheduleEntry[] {
  const gameEntries: ScheduleEntry[] = phaseGroup.games.map((g) => ({
    type: 'game' as const,
    time: g.startTime ?? '',
    game: g,
  }))

  if (!showBreaks.value) {
    return gameEntries.sort((a, b) => a.time.localeCompare(b.time))
  }

  // Find break slots that fall between this group's first and last game
  const sortedGames = [...gameEntries].sort((a, b) => a.time.localeCompare(b.time))
  if (sortedGames.length === 0) return []

  const firstTime = sortedGames[0].time
  const lastTime = sortedGames[sortedGames.length - 1].time

  const breakEntries: ScheduleEntry[] = [...allTimeSlots.value]
    .filter((slot) => slot >= firstTime && slot <= lastTime && !teamTimeSlots.value.has(slot))
    .map((slot) => ({ type: 'break' as const, time: slot }))

  return [...sortedGames, ...breakEntries].sort((a, b) => a.time.localeCompare(b.time))
}

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

      <div v-for="group in groupedGames" :key="`${group.phaseId}:${group.groupId}`" class="mb-5">
        <h3 class="text-h6 mb-3">
          {{ group.phaseName }}
          <span v-if="group.levelName" class="text-medium-emphasis font-weight-regular">
            — {{ group.levelName }}</span
          >
          <span class="text-medium-emphasis font-weight-regular">
            — Group {{ group.groupName }}</span
          >
        </h3>

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
            <template
              v-for="entry in phaseGroupEntries(group)"
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
                  <v-chip size="small" :color="roleColor(teamRole(entry.game!))" variant="tonal">
                    {{ teamRole(entry.game!) }}
                  </v-chip>
                </td>
              </tr>
              <tr v-else class="break-row">
                <td>{{ formatTime(entry.time) }}</td>
                <td colspan="4" class="text-medium-emphasis text-italic">Break</td>
              </tr>
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
      @saved="onResultSaved"
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
</style>
