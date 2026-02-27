export interface GroupDto {
  id?: string
  name: string
  teamIds?: string[]
}

export interface PhaseDto {
  id?: string
  name: string
  format: string
  order?: number
  numberOfGroups?: number
  groups?: GroupDto[]
}

export interface TournamentStructureDto {
  id?: string
  tournamentId?: string
  phases?: PhaseDto[]
}

export interface TeamAssignmentRequest {
  teamId: string
}

export const PHASE_FORMATS = [
  { value: 'RoundRobin', title: 'Round Robin' },
  { value: 'PlayoffElimination', title: 'Playoff Elimination' },
  { value: 'PlayoffWithPlacement', title: 'Playoff with Placement' },
] as const

export function formatPhaseFormat(format: string): string {
  return PHASE_FORMATS.find(f => f.value === format)?.title ?? format
}
