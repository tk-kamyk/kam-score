import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type { VolunteerDto, ShiftGroupDto, VolunteerAvailabilityDto } from '@/volunteer/types'

export const useVolunteerStore = defineStore('volunteer', () => {
  const volunteers = ref<VolunteerDto[]>([])
  const loading = ref(false)
  const shiftGroups = ref<ShiftGroupDto[]>([])
  const shiftsLoading = ref(false)

  async function fetchVolunteers(tournamentId: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<VolunteerDto[]>(
        `/tournaments/${tournamentId}/volunteers`,
      )
      volunteers.value = data
    } finally {
      loading.value = false
    }
  }

  async function createVolunteer(tournamentId: string, dto: VolunteerDto): Promise<VolunteerDto> {
    const { data } = await apiClient.post<VolunteerDto>(
      `/tournaments/${tournamentId}/volunteers`,
      dto,
    )
    volunteers.value = [...volunteers.value, data]
    return data
  }

  async function updateVolunteer(
    tournamentId: string,
    volunteerId: string,
    dto: VolunteerDto,
  ): Promise<VolunteerDto> {
    const { data } = await apiClient.put<VolunteerDto>(
      `/tournaments/${tournamentId}/volunteers/${volunteerId}`,
      dto,
    )
    const index = volunteers.value.findIndex((v) => v.id === volunteerId)
    if (index >= 0) {
      volunteers.value[index] = data
    }
    return data
  }

  async function deleteVolunteer(tournamentId: string, volunteerId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/volunteers/${volunteerId}`)
    volunteers.value = volunteers.value.filter((v) => v.id !== volunteerId)
  }

  async function fetchShifts(tournamentId: string) {
    shiftsLoading.value = true
    try {
      const { data } = await apiClient.get<ShiftGroupDto[]>(
        `/tournaments/${tournamentId}/volunteers/shifts`,
      )
      shiftGroups.value = data
    } finally {
      shiftsLoading.value = false
    }
  }

  async function fetchAvailableVolunteers(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
  ): Promise<VolunteerAvailabilityDto[]> {
    const url = shiftTime
      ? `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/${encodeURIComponent(shiftTime)}/available`
      : `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/available`
    const { data } = await apiClient.get<VolunteerAvailabilityDto[]>(url)
    return data
  }

  async function assignVolunteer(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
    volunteerId: string,
  ) {
    const url = shiftTime
      ? `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/${encodeURIComponent(shiftTime)}/assign/${volunteerId}`
      : `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/assign/${volunteerId}`
    await apiClient.post(url)
  }

  async function unassignVolunteer(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
    volunteerId: string,
  ) {
    const url = shiftTime
      ? `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/${encodeURIComponent(shiftTime)}/assign/${volunteerId}`
      : `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/assign/${volunteerId}`
    await apiClient.delete(url)
  }

  return {
    volunteers,
    loading,
    shiftGroups,
    shiftsLoading,
    fetchVolunteers,
    createVolunteer,
    updateVolunteer,
    deleteVolunteer,
    fetchShifts,
    fetchAvailableVolunteers,
    assignVolunteer,
    unassignVolunteer,
  }
})
