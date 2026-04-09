import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { VolunteerDto } from '@/volunteer/types'

export const useVolunteerStore = defineStore('volunteer', () => {
  const volunteers = ref<VolunteerDto[]>([])
  const loading = ref(false)

  async function fetchVolunteers(tournamentId: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<VolunteerDto[]>(`/tournaments/${tournamentId}/volunteers`)
      volunteers.value = data
    } finally {
      loading.value = false
    }
  }

  async function createVolunteer(tournamentId: string, dto: VolunteerDto): Promise<VolunteerDto> {
    const { data } = await apiClient.post<VolunteerDto>(`/tournaments/${tournamentId}/volunteers`, dto)
    volunteers.value = [...volunteers.value, data]
    return data
  }

  async function updateVolunteer(tournamentId: string, volunteerId: string, dto: VolunteerDto): Promise<VolunteerDto> {
    const { data } = await apiClient.put<VolunteerDto>(`/tournaments/${tournamentId}/volunteers/${volunteerId}`, dto)
    const index = volunteers.value.findIndex(v => v.id === volunteerId)
    if (index >= 0) {
      volunteers.value[index] = data
    }
    return data
  }

  async function deleteVolunteer(tournamentId: string, volunteerId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/volunteers/${volunteerId}`)
    volunteers.value = volunteers.value.filter(v => v.id !== volunteerId)
  }

  return {
    volunteers,
    loading,
    fetchVolunteers,
    createVolunteer,
    updateVolunteer,
    deleteVolunteer,
  }
})
