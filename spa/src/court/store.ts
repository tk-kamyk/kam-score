import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { CourtDto } from '@/court/types'

export const useCourtStore = defineStore('court', () => {
  const courts = ref<CourtDto[]>([])
  const loading = ref(false)

  async function fetchCourts(tournamentId: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<CourtDto[]>(`/tournaments/${tournamentId}/courts`)
      courts.value = data
    } finally {
      loading.value = false
    }
  }

  async function createCourt(tournamentId: string, dto: CourtDto): Promise<CourtDto> {
    const { data } = await apiClient.post<CourtDto>(`/tournaments/${tournamentId}/courts`, dto)
    courts.value.push(data)
    return data
  }

  async function updateCourt(tournamentId: string, courtId: string, dto: CourtDto): Promise<CourtDto> {
    const { data } = await apiClient.put<CourtDto>(`/tournaments/${tournamentId}/courts/${courtId}`, dto)
    const index = courts.value.findIndex(c => c.id === courtId)
    if (index >= 0) {
      courts.value[index] = data
    }
    return data
  }

  async function deleteCourt(tournamentId: string, courtId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/courts/${courtId}`)
    courts.value = courts.value.filter(c => c.id !== courtId)
  }

  return {
    courts,
    loading,
    fetchCourts,
    createCourt,
    updateCourt,
    deleteCourt,
  }
})
