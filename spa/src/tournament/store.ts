import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import apiClient from '@/api/client'
import { useAuthStore } from '@/auth/store'
import type { TournamentDto } from '@/tournament/types'

export const useTournamentStore = defineStore('tournament', () => {
  const auth = useAuthStore()
  const tournaments = ref<TournamentDto[]>([])
  const currentTournament = ref<TournamentDto | null>(null)
  const loading = ref(false)

  watch(() => auth.isAuthenticated, () => {
    fetchTournaments()
  })

  async function fetchTournaments() {
    loading.value = true
    try {
      const { data } = await apiClient.get<TournamentDto[]>('/tournaments')
      tournaments.value = data
    } finally {
      loading.value = false
    }
  }

  async function fetchTournament(id: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<TournamentDto>(`/tournaments/${id}`)
      currentTournament.value = data
    } finally {
      loading.value = false
    }
  }

  async function createTournament(dto: TournamentDto): Promise<TournamentDto> {
    const { data } = await apiClient.post<TournamentDto>('/tournaments', dto)
    tournaments.value.push(data)
    return data
  }

  async function updateTournament(id: string, dto: TournamentDto): Promise<TournamentDto> {
    const { data } = await apiClient.put<TournamentDto>(`/tournaments/${id}`, dto)
    const index = tournaments.value.findIndex(t => t.id === id)
    if (index >= 0) {
      tournaments.value[index] = data
    }
    currentTournament.value = data
    return data
  }

  async function deleteTournament(id: string) {
    await apiClient.delete(`/tournaments/${id}`)
    tournaments.value = tournaments.value.filter(t => t.id !== id)
    if (currentTournament.value?.id === id) {
      currentTournament.value = null
    }
  }

  return {
    tournaments,
    currentTournament,
    loading,
    fetchTournaments,
    fetchTournament,
    createTournament,
    updateTournament,
    deleteTournament,
  }
})
