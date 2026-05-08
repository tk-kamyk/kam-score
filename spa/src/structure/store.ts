import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import { useGameStore } from '@/game/store'
import { useTeamStore } from '@/team/store'
import { useStandingsStore } from '@/standings/store'
import type {
  TournamentStructureDto,
  PhaseDto,
  GroupDto,
  LevelDto,
  TeamAssignmentRequest,
} from '@/structure/types'

export const useStructureStore = defineStore('structure', () => {
  const structure = ref<TournamentStructureDto | null>(null)
  const loading = ref(false)

  function replacePhase(phaseId: string, data: PhaseDto) {
    if (!structure.value?.phases) return
    const index = structure.value.phases.findIndex((p) => p.id === phaseId)
    if (index >= 0) {
      structure.value.phases[index] = data
    }
  }

  async function fetchStructure(tournamentId: string) {
    loading.value = true
    try {
      const { data } = await apiClient.get<TournamentStructureDto>(
        `/tournaments/${tournamentId}/structure`,
      )
      structure.value = data
    } finally {
      loading.value = false
    }
  }

  async function addPhase(tournamentId: string, dto: PhaseDto): Promise<PhaseDto> {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases`,
      dto,
    )
    if (structure.value?.phases) {
      structure.value.phases = [...structure.value.phases, data]
    }
    // Cascade: backend may create placeholder teams for the new phase.
    await useTeamStore().fetchPlaceholders(tournamentId)
    return data
  }

  async function updatePhase(
    tournamentId: string,
    phaseId: string,
    dto: PhaseDto,
  ): Promise<PhaseDto> {
    const { data } = await apiClient.put<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}`,
      dto,
    )
    replacePhase(phaseId, data)
    // Cascade: phase name is denormalized into GameDto; backend may regenerate placeholder teams.
    await Promise.all([
      useGameStore().fetchGames(tournamentId),
      useTeamStore().fetchPlaceholders(tournamentId),
    ])
    return data
  }

  async function deletePhase(tournamentId: string, phaseId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/structure/phases/${phaseId}`)
    if (structure.value?.phases) {
      structure.value.phases = structure.value.phases
        .filter((p) => p.id !== phaseId)
        .map((p, i) => ({ ...p, order: i + 1 }))
    }
    // Cascade: backend deletes the phase's games + its placeholder teams; standings keys for the phase invalid.
    const standingsStore = useStandingsStore()
    standingsStore.invalidatePhase(phaseId)
    await Promise.all([
      useGameStore().fetchGames(tournamentId),
      useTeamStore().fetchPlaceholders(tournamentId),
      standingsStore.fetchFinalStandings(tournamentId),
    ])
  }

  async function addGroup(tournamentId: string, phaseId: string, dto: GroupDto): Promise<GroupDto> {
    const { data } = await apiClient.post<GroupDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups`,
      dto,
    )
    const phase = structure.value?.phases?.find((p) => p.id === phaseId)
    if (phase?.groups) {
      phase.groups = [...phase.groups, data]
    }
    return data
  }

  async function updateGroup(
    tournamentId: string,
    phaseId: string,
    groupId: string,
    dto: GroupDto,
  ): Promise<GroupDto> {
    const { data } = await apiClient.put<GroupDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups/${groupId}`,
      dto,
    )
    const phase = structure.value?.phases?.find((p) => p.id === phaseId)
    if (phase?.groups) {
      const index = phase.groups.findIndex((g) => g.id === groupId)
      if (index >= 0) {
        phase.groups[index] = data
      }
    }
    // Cascade: group name is denormalized into GameDto.
    await useGameStore().fetchGames(tournamentId)
    return data
  }

  async function deleteGroup(tournamentId: string, phaseId: string, groupId: string) {
    await apiClient.delete(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups/${groupId}`,
    )
    const phase = structure.value?.phases?.find((p) => p.id === phaseId)
    if (phase?.groups) {
      phase.groups = phase.groups.filter((g) => g.id !== groupId)
    }
    // Cascade: standings cache for this phase/group is stale.
    useStandingsStore().invalidateGroup(phaseId, groupId)
  }

  async function assignTeam(
    tournamentId: string,
    phaseId: string,
    groupId: string,
    teamId: string,
  ) {
    const request: TeamAssignmentRequest = { teamId }
    await apiClient.post(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups/${groupId}/teams`,
      request,
    )
    const phase = structure.value?.phases?.find((p) => p.id === phaseId)
    const group = phase?.groups?.find((g) => g.id === groupId)
    if (group?.teamIds) {
      group.teamIds = [...group.teamIds, teamId]
    }
  }

  async function removeTeam(
    tournamentId: string,
    phaseId: string,
    groupId: string,
    teamId: string,
  ) {
    await apiClient.delete(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups/${groupId}/teams/${teamId}`,
    )
    const phase = structure.value?.phases?.find((p) => p.id === phaseId)
    const group = phase?.groups?.find((g) => g.id === groupId)
    if (group?.teamIds) {
      group.teamIds = group.teamIds.filter((id) => id !== teamId)
    }
  }

  async function autoAssignTeams(tournamentId: string, phaseId: string) {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/auto-assign`,
    )
    replacePhase(phaseId, data)
    return data
  }

  async function completePhase(tournamentId: string, phaseId: string) {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/complete`,
    )
    // Cascade: backend resolves placeholders into next phase's games + sets resolvedTeamId on
    // placeholder teams + may activate the next phase. A single replacePhase is not enough —
    // the next phase's status and games change too.
    await Promise.all([
      fetchStructure(tournamentId),
      useGameStore().fetchGames(tournamentId),
      useTeamStore().fetchPlaceholders(tournamentId),
      useStandingsStore().fetchFinalStandings(tournamentId),
    ])
    return data
  }

  async function updateLevel(
    tournamentId: string,
    phaseId: string,
    levelId: string,
    dto: LevelDto,
  ): Promise<LevelDto> {
    const { data } = await apiClient.put<LevelDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/levels/${levelId}`,
      dto,
    )
    const phase = structure.value?.phases?.find((p) => p.id === phaseId)
    if (phase?.levels) {
      const index = phase.levels.findIndex((l) => l.id === levelId)
      if (index >= 0) {
        phase.levels[index] = data
      }
    }
    // Cascade: level name is denormalized into GameDto.
    await useGameStore().fetchGames(tournamentId)
    return data
  }

  async function reopenPhase(tournamentId: string, phaseId: string) {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/reopen`,
    )
    // Cascade: reverses completePhase — placeholders are unresolved, next phase status reverts.
    await Promise.all([
      fetchStructure(tournamentId),
      useGameStore().fetchGames(tournamentId),
      useTeamStore().fetchPlaceholders(tournamentId),
      useStandingsStore().fetchFinalStandings(tournamentId),
    ])
    return data
  }

  function reset() {
    structure.value = null
    loading.value = false
  }

  return {
    structure,
    loading,
    fetchStructure,
    addPhase,
    updatePhase,
    deletePhase,
    addGroup,
    updateGroup,
    deleteGroup,
    assignTeam,
    removeTeam,
    updateLevel,
    autoAssignTeams,
    completePhase,
    reopenPhase,
    reset,
  }
})
