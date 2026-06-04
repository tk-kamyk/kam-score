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

  // Both regular (with time) and special (Set-up/Cleanup, no time) assign/unassign hit this route;
  // the only difference is whether the time segment is present.
  function shiftAssignUrl(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
    volunteerId: string,
  ) {
    const base = `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}`
    return shiftTime
      ? `${base}/${encodeURIComponent(shiftTime)}/assign/${volunteerId}`
      : `${base}/assign/${volunteerId}`
  }

  async function assignVolunteer(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
    volunteerId: string,
  ) {
    await apiClient.post(shiftAssignUrl(tournamentId, shiftGroup, shiftTime, volunteerId))
    await fetchShifts(tournamentId)
  }

  // Sets/clears the station colour on an assignment. Hits the same upsert assign endpoint with
  // a body — station = null clears the colour. (A bare assignVolunteer sends no body and so
  // leaves any existing colour untouched.)
  async function setVolunteerStation(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
    volunteerId: string,
    station: number | null,
  ) {
    await apiClient.post(shiftAssignUrl(tournamentId, shiftGroup, shiftTime, volunteerId), {
      station,
    })
    await fetchShifts(tournamentId)
  }

  async function unassignVolunteer(
    tournamentId: string,
    shiftGroup: string,
    shiftTime: string | null,
    volunteerId: string,
  ) {
    await apiClient.delete(shiftAssignUrl(tournamentId, shiftGroup, shiftTime, volunteerId))
    await fetchShifts(tournamentId)
  }

  async function clearShiftGroupAssignments(tournamentId: string, shiftGroup: string) {
    await apiClient.delete(
      `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/assignments`,
    )
    await fetchShifts(tournamentId)
  }

  async function autoAssignShiftGroup(
    tournamentId: string,
    shiftGroup: string,
    volunteersPerShift: number,
    stationCount: number | null = null,
  ) {
    await apiClient.post(
      `/tournaments/${tournamentId}/volunteers/shifts/${encodeURIComponent(shiftGroup)}/auto-assign`,
      { volunteersPerShift, stationCount },
    )
    await fetchShifts(tournamentId)
  }

  function reset() {
    volunteers.value = []
    shiftGroups.value = []
    loading.value = false
    shiftsLoading.value = false
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
    setVolunteerStation,
    unassignVolunteer,
    clearShiftGroupAssignments,
    autoAssignShiftGroup,
    reset,
  }
})
