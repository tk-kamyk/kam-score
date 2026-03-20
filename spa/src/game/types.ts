export interface SetResultDto {
  homePoints: number
  awayPoints: number
}

export interface GameDto {
  id?: string
  phaseId?: string
  groupId?: string
  round?: number
  label?: string
  homeTeamId?: string
  awayTeamId?: string
  homeTeamPlaceholder?: string
  awayTeamPlaceholder?: string
  refereeTeamId?: string
  courtId?: string
  startTime?: string
  status?: string
  homeScore?: number
  awayScore?: number
  sets?: SetResultDto[]
  homeTeamName?: string
  awayTeamName?: string
  refereeTeamName?: string
  courtName?: string
  homeTeamIsPlaceholder?: boolean
  awayTeamIsPlaceholder?: boolean
  phaseName?: string
  groupName?: string
  levelName?: string
}

export interface GameResultInput {
  sets?: SetResultDto[]
  homeScore?: number
  awayScore?: number
}

export interface RefereeCandidateDto {
  teamId: string
  teamName: string
}
