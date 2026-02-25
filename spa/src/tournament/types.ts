export interface GameConditionsDto {
  winningSets?: number
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
}
