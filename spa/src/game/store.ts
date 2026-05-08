import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import { useStandingsStore } from '@/standings/store'
import { useStructureStore } from '@/structure/store'
import type { GameDto, GameResultInput, RefereeCandidateDto } from '@/game/types'

export const useGameStore = defineStore('game', () => {
  const games = ref<GameDto[]>([])
  const loading = ref(false)

  async function fetchGames(
    tournamentId: string,
    phaseId?: string,
    groupId?: string,
    courtId?: string,
  ) {
    loading.value = true
    try {
      const params = new URLSearchParams()
      if (phaseId) params.set('phaseId', phaseId)
      if (groupId) params.set('groupId', groupId)
      if (courtId) params.set('courtId', courtId)
      const query = params.toString()
      const url = `/tournaments/${tournamentId}/games${query ? `?${query}` : ''}`
      const { data } = await apiClient.get<GameDto[]>(url)
      games.value = data
    } finally {
      loading.value = false
    }
  }

  async function generateSchedule(tournamentId: string, phaseId: string): Promise<GameDto[]> {
    const { data } = await apiClient.post<GameDto[]>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/generate-schedule`,
    )
    games.value = [...games.value.filter((g) => g.phaseId !== phaseId), ...data]
    // Cascade: phase status transitions to Scheduled / InProgress on the backend.
    await useStructureStore().fetchStructure(tournamentId)
    return data
  }

  async function deleteGames(tournamentId: string, phaseId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/structure/phases/${phaseId}/games`)
    // Self: drop the phase's games from cache.
    games.value = games.value.filter((g) => g.phaseId !== phaseId)
    // Cascade: phase status reverts to New; standings for the phase are no longer valid.
    const standingsStore = useStandingsStore()
    standingsStore.invalidatePhase(phaseId)
    await Promise.all([
      useStructureStore().fetchStructure(tournamentId),
      standingsStore.fetchFinalStandings(tournamentId),
    ])
  }

  async function recordResult(
    tournamentId: string,
    gameId: string,
    result: GameResultInput,
    tournamentCode?: string,
  ) {
    const headers: Record<string, string> = {}
    if (tournamentCode) {
      headers['X-Tournament-Code'] = tournamentCode
    }
    const game = games.value.find((g) => g.id === gameId)
    const phaseId = game?.phaseId
    const groupId = game?.groupId
    await apiClient.put<GameDto>(`/tournaments/${tournamentId}/games/${gameId}/result`, result, {
      headers,
    })
    await fetchGames(tournamentId)
    // Cascade: standings for affected phase/group + final standings; structure phase status may auto-advance.
    const standingsStore = useStandingsStore()
    const tasks: Promise<unknown>[] = [
      standingsStore.fetchFinalStandings(tournamentId),
      useStructureStore().fetchStructure(tournamentId),
    ]
    if (phaseId && groupId) {
      tasks.push(standingsStore.fetchStandings(tournamentId, phaseId, groupId))
    }
    await Promise.all(tasks)
  }

  async function fetchRefereeCandidates(
    tournamentId: string,
    gameId: string,
  ): Promise<RefereeCandidateDto[]> {
    const { data } = await apiClient.get<RefereeCandidateDto[]>(
      `/tournaments/${tournamentId}/games/${gameId}/referee-candidates`,
    )
    return data
  }

  async function assignReferee(
    tournamentId: string,
    gameId: string,
    teamId: string,
    isPlaceholder?: boolean,
  ) {
    const body = isPlaceholder ? { placeholder: teamId } : { teamId }
    await apiClient.put(`/tournaments/${tournamentId}/games/${gameId}/referee`, body)
    await fetchGames(tournamentId)
  }

  function reset() {
    games.value = []
    loading.value = false
  }

  return {
    games,
    loading,
    fetchGames,
    generateSchedule,
    deleteGames,
    recordResult,
    fetchRefereeCandidates,
    assignReferee,
    reset,
  }
})
