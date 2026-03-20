import { ref } from 'vue'
import type { GameDto } from '@/game/types'

export function useRefereeDialog() {
  const showRefereeDialog = ref(false)
  const refereeGame = ref<GameDto | null>(null)

  function openRefereeDialog(game: GameDto) {
    refereeGame.value = game
    showRefereeDialog.value = true
  }

  return { showRefereeDialog, refereeGame, openRefereeDialog }
}
