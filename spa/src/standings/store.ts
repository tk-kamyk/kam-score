import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { StandingDto, FinalStandingDto } from '@/standings/types'

export const useStandingsStore = defineStore('standings', () => {
  const standings = ref<Record<string, StandingDto[]>>({})
  const loading = ref(false)
  const saving = ref(false)

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

  async function saveManualStandings(
    tournamentId: string,
    phaseId: string,
    groupId: string,
    orderedTeamIds: string[],
  ) {
    saving.value = true
    try {
      const { data } = await apiClient.put<StandingDto[]>(
        `/tournaments/${tournamentId}/standings`,
        { phaseId, groupId, orderedTeamIds },
      )
      standings.value[`${phaseId}:${groupId}`] = data
    } finally {
      saving.value = false
    }
  }

  const finalStandings = ref<FinalStandingDto[]>([])
  const finalStandingsLoading = ref(false)

  async function fetchFinalStandings(tournamentId: string) {
    finalStandingsLoading.value = true
    try {
      const { data } = await apiClient.get<FinalStandingDto[]>(
        `/tournaments/${tournamentId}/final-standings`,
      )
      finalStandings.value = data
    } finally {
      finalStandingsLoading.value = false
    }
  }

  return {
    standings,
    loading,
    saving,
    fetchStandings,
    getStandings,
    saveManualStandings,
    finalStandings,
    finalStandingsLoading,
    fetchFinalStandings,
  }
})
