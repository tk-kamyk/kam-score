import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { StandingDto } from '@/standings/types'

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

  return {
    standings,
    loading,
    fetchStandings,
    getStandings,
  }
})
