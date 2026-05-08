import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiClient from '@/api/client'
import { useGameStore } from '@/game/store'
import { useStandingsStore } from '@/standings/store'
import { useVolunteerStore } from '@/volunteer/store'
import type { TeamDto } from '@/team/types'

export const useTeamStore = defineStore('team', () => {
  const teams = ref<TeamDto[]>([])
  const placeholders = ref<TeamDto[]>([])
  const loading = ref(false)

  const teamsWithPlaceholders = computed(() => [...teams.value, ...placeholders.value])

  async function fetchTeams(tournamentId: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<TeamDto[]>(`/tournaments/${tournamentId}/teams`)
      teams.value = data
    } finally {
      loading.value = false
    }
  }

  async function fetchPlaceholders(tournamentId: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<TeamDto[]>(`/tournaments/${tournamentId}/teams`, {
        params: { includePlaceholders: true },
      })
      placeholders.value = data.filter((t) => t.isPlaceholder)
    } finally {
      loading.value = false
    }
  }

  async function createTeam(tournamentId: string, dto: TeamDto): Promise<TeamDto> {
    const { data } = await apiClient.post<TeamDto>(`/tournaments/${tournamentId}/teams`, dto)
    teams.value = [...teams.value, data]
    return data
  }

  async function updateTeam(tournamentId: string, teamId: string, dto: TeamDto): Promise<TeamDto> {
    const { data } = await apiClient.put<TeamDto>(
      `/tournaments/${tournamentId}/teams/${teamId}`,
      dto,
    )
    const index = teams.value.findIndex((t) => t.id === teamId)
    if (index >= 0) {
      teams.value[index] = data
    }
    // Cascade: team name is denormalized into GameDto and StandingDto.
    const gameStore = useGameStore()
    const standingsStore = useStandingsStore()
    standingsStore.invalidateAll()
    await Promise.all([
      gameStore.fetchGames(tournamentId),
      standingsStore.fetchFinalStandings(tournamentId),
    ])
    return data
  }

  async function deleteTeam(tournamentId: string, teamId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/teams/${teamId}`)
    teams.value = teams.value.filter((t) => t.id !== teamId)
    // Cascade: backend clears any volunteer's teamId reference.
    const volunteerStore = useVolunteerStore()
    await volunteerStore.fetchVolunteers(tournamentId)
  }

  async function generateSeedTeams(tournamentId: string, count: number): Promise<TeamDto[]> {
    const { data } = await apiClient.post<TeamDto[]>(
      `/tournaments/${tournamentId}/teams/generate`,
      { count },
    )
    teams.value = [...teams.value, ...data]
    return data
  }

  function reset() {
    teams.value = []
    placeholders.value = []
    loading.value = false
  }

  return {
    teams,
    placeholders,
    teamsWithPlaceholders,
    loading,
    fetchTeams,
    fetchPlaceholders,
    createTeam,
    updateTeam,
    deleteTeam,
    generateSeedTeams,
    reset,
  }
})
