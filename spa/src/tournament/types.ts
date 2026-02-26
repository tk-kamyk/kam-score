export interface GameConditionsDto {
  bestOfSets?: number
  pointsPerSet?: number[]
}

export interface TournamentDto {
  id?: string
  name: string
  discipline: string
  startTime?: string
  gameLength?: number
  gameConditions?: GameConditionsDto
  tournamentCode?: string
  ownerId?: string
  teamCount?: number
  courtCount?: number
}
