import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { StandingDto, FinalStandingsResponse } from '@/standings/types'

export const useStandingsStore = defineStore('standings', () => {
  const standings = ref<Record<string, StandingDto[]>>({})
  const loading = ref(false)

  async function fetchStandings(tournamentId: string, phaseId: string, groupId: string) {
    loading.value = true
    try {
      const params = new URLSearchParams({ phaseId, groupId })
      const { data } = await apiClient.get<StandingDto[]>(
        `/tournaments/${tournamentId}/standings?${params}`,
      )
      standings.value[`${phaseId}:${groupId}`] = data
    } finally {
      loading.value = false
    }
  }

  function getStandings(phaseId: string, groupId: string): StandingDto[] {
    return standings.value[`${phaseId}:${groupId}`] ?? []
  }

  const finalStandings = ref<FinalStandingsResponse | null>(null)
  const finalStandingsLoading = ref(false)

  async function fetchFinalStandings(tournamentId: string) {
    finalStandingsLoading.value = true
    try {
      // TODO: Replace with real API call in iteration 3
      // const { data } = await apiClient.get<FinalStandingsResponse>(
      //   `/tournaments/${tournamentId}/final-standings`,
      // )
      // finalStandings.value = data
      void tournamentId
      finalStandings.value = {
        provisional: true,
        standings: [
          { position: 1, teamId: '1', teamName: 'Eagles', levelName: 'Gold' },
          { position: 2, teamId: '2', teamName: 'Hawks', levelName: 'Gold' },
          { position: 3, teamId: '3', teamName: 'Wolves', levelName: 'Gold' },
          { position: 4, teamId: '4', teamName: 'Bears', levelName: 'Gold' },
          { position: 1, teamId: '5', teamName: 'Lions', levelName: 'Silver' },
          { position: 2, teamId: '6', teamName: 'Tigers', levelName: 'Silver' },
          { position: 3, teamId: '7', teamName: 'Panthers', levelName: 'Silver' },
          { position: 4, teamId: '8', teamName: 'Falcons', levelName: 'Silver' },
        ],
      }
    } finally {
      finalStandingsLoading.value = false
    }
  }

  return {
    standings,
    loading,
    fetchStandings,
    getStandings,
    finalStandings,
    finalStandingsLoading,
    fetchFinalStandings,
  }
})
