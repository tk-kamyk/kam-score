import { computed } from 'vue'
import { useGameStore } from '@/game/store'
import type { GameDto } from '@/game/types'

export function useGamesByPhase() {
  const gameStore = useGameStore()

  const gamesByPhase = computed(() => {
    const map: Record<string, GameDto[]> = {}
    for (const game of gameStore.games) {
      const key = game.phaseId ?? ''
      if (!map[key]) map[key] = []
      map[key].push(game)
    }
    return map
  })

  const gamesByCourt = computed(() => {
    const map: Record<string, GameDto[]> = {}
    for (const game of gameStore.games) {
      const key = game.courtId ?? ''
      if (!map[key]) map[key] = []
      map[key].push(game)
    }
    return map
  })

  function phaseGames(phaseId: string): GameDto[] {
    return gamesByPhase.value[phaseId] ?? []
  }

  function courtGames(courtId: string): GameDto[] {
    return gamesByCourt.value[courtId] ?? []
  }

  function teamGames(teamId: string): GameDto[] {
    return gameStore.games.filter(
      (g) => g.homeTeamId === teamId || g.awayTeamId === teamId || g.refereeTeamId === teamId,
    )
  }

  return { gamesByPhase, gamesByCourt, phaseGames, courtGames, teamGames }
}
