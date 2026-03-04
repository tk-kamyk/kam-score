import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '@/api/client'
import type {
  TournamentStructureDto,
  PhaseDto,
  GroupDto,
  TeamAssignmentRequest,
} from '@/structure/types'

export const useStructureStore = defineStore('structure', () => {
  const structure = ref<TournamentStructureDto | null>(null)
  const loading = ref(false)

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

  async function initializeStructure(tournamentId: string) {
    const { data } = await apiClient.post<TournamentStructureDto>(
      `/tournaments/${tournamentId}/structure`,
    )
    structure.value = data
    return data
  }

  async function addPhase(tournamentId: string, dto: PhaseDto): Promise<PhaseDto> {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases`,
      dto,
    )
    structure.value?.phases?.push(data)
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
    if (structure.value?.phases) {
      const index = structure.value.phases.findIndex(p => p.id === phaseId)
      if (index >= 0) {
        structure.value.phases[index] = data
      }
    }
    return data
  }

  async function deletePhase(tournamentId: string, phaseId: string) {
    await apiClient.delete(`/tournaments/${tournamentId}/structure/phases/${phaseId}`)
    if (structure.value?.phases) {
      structure.value.phases = structure.value.phases.filter(p => p.id !== phaseId)
      structure.value.phases.forEach((p, i) => {
        p.order = i + 1
      })
    }
  }

  async function addGroup(
    tournamentId: string,
    phaseId: string,
    dto: GroupDto,
  ): Promise<GroupDto> {
    const { data } = await apiClient.post<GroupDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups`,
      dto,
    )
    const phase = structure.value?.phases?.find(p => p.id === phaseId)
    phase?.groups?.push(data)
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
    const phase = structure.value?.phases?.find(p => p.id === phaseId)
    if (phase?.groups) {
      const index = phase.groups.findIndex(g => g.id === groupId)
      if (index >= 0) {
        phase.groups[index] = data
      }
    }
    return data
  }

  async function deleteGroup(tournamentId: string, phaseId: string, groupId: string) {
    await apiClient.delete(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/groups/${groupId}`,
    )
    const phase = structure.value?.phases?.find(p => p.id === phaseId)
    if (phase?.groups) {
      phase.groups = phase.groups.filter(g => g.id !== groupId)
    }
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
    const phase = structure.value?.phases?.find(p => p.id === phaseId)
    const group = phase?.groups?.find(g => g.id === groupId)
    group?.teamIds?.push(teamId)
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
    const phase = structure.value?.phases?.find(p => p.id === phaseId)
    const group = phase?.groups?.find(g => g.id === groupId)
    if (group?.teamIds) {
      group.teamIds = group.teamIds.filter(id => id !== teamId)
    }
  }

  async function autoAssignTeams(tournamentId: string, phaseId: string) {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/auto-assign`,
    )
    if (structure.value?.phases) {
      const index = structure.value.phases.findIndex(p => p.id === phaseId)
      if (index >= 0) {
        structure.value.phases[index] = data
      }
    }
    return data
  }

  async function completePhase(tournamentId: string, phaseId: string) {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/complete`,
    )
    if (structure.value?.phases) {
      const index = structure.value.phases.findIndex(p => p.id === phaseId)
      if (index >= 0) {
        structure.value.phases[index] = data
      }
    }
    return data
  }

  async function reopenPhase(tournamentId: string, phaseId: string) {
    const { data } = await apiClient.post<PhaseDto>(
      `/tournaments/${tournamentId}/structure/phases/${phaseId}/reopen`,
    )
    if (structure.value?.phases) {
      const index = structure.value.phases.findIndex(p => p.id === phaseId)
      if (index >= 0) {
        structure.value.phases[index] = data
      }
    }
    return data
  }

  return {
    structure,
    loading,
    fetchStructure,
    initializeStructure,
    addPhase,
    updatePhase,
    deletePhase,
    addGroup,
    updateGroup,
    deleteGroup,
    assignTeam,
    removeTeam,
    autoAssignTeams,
    completePhase,
    reopenPhase,
  }
})
