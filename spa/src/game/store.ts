import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { GameDto, GameResultInput } from '@/game/types'

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
    return data
  }

  async function deleteGames(tournamentId: string, phaseId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/structure/phases/${phaseId}/games`)
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
    await apiClient.put<GameDto>(
      `/tournaments/${tournamentId}/games/${gameId}/result`,
      result,
      { headers },
    )
    await fetchGames(tournamentId)
  }

  return {
    games,
    loading,
    fetchGames,
    generateSchedule,
    deleteGames,
    recordResult,
  }
})
