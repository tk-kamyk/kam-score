import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { VolunteerDto } from '@/volunteer/types'

export const useVolunteerStore = defineStore('volunteer', () => {
  const volunteers = ref<VolunteerDto[]>([])
  const loading = ref(false)

  async function fetchVolunteers(_tournamentId: string) {
    loading.value = true
    try {
      // TODO: Replace with API call in Gate 6
      volunteers.value = [
        { id: '1', name: 'John Doe', contact: 'john@email.com', teamId: 't1', teamName: 'Eagles' },
        { id: '2', name: 'Jane Smith', contact: null, teamId: null, teamName: null },
        { id: '3', name: 'Bob Wilson', contact: '+123456789', teamId: 't2', teamName: 'Hawks' },
      ]
    } finally {
      loading.value = false
    }
  }

  async function createVolunteer(_tournamentId: string, dto: VolunteerDto): Promise<VolunteerDto> {
    // TODO: Replace with API call in Gate 6
    const created = { ...dto, id: crypto.randomUUID() }
    volunteers.value = [...volunteers.value, created]
    return created
  }

  async function updateVolunteer(_tournamentId: string, volunteerId: string, dto: VolunteerDto): Promise<VolunteerDto> {
    // TODO: Replace with API call in Gate 6
    const updated = { ...dto, id: volunteerId }
    const index = volunteers.value.findIndex(v => v.id === volunteerId)
    if (index >= 0) {
      volunteers.value[index] = updated
    }
    return updated
  }

  async function deleteVolunteer(_tournamentId: string, volunteerId: string) {
    // TODO: Replace with API call in Gate 6
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
