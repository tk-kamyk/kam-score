export interface GameConditionsDto {
  bestOfSets?: number
  pointsPerSet?: number[]
}

export type TournamentType = 'Public' | 'Private' | 'Template'

export const TOURNAMENT_TYPES: TournamentType[] = ['Public', 'Private', 'Template']

export interface TournamentDto {
  id?: string
  name: string
  discipline: string
  type?: TournamentType
  startTime?: string
  gameLength?: number
  gameConditions?: GameConditionsDto
  tournamentCode?: string
  ownerId?: string
  lastModified?: string
  teamCount?: number
  courtCount?: number
  sourceTournamentId?: string
  ownerDisplayName?: string
}
