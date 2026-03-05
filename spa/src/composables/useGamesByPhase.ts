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

  function phaseGames(phaseId: string): GameDto[] {
    return gamesByPhase.value[phaseId] ?? []
  }

  return { gamesByPhase, phaseGames }
}
