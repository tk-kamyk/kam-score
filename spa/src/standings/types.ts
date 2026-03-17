export interface StandingDto {
  teamId: string
  teamName?: string
  position: number
  gamesPlayed: number
  wins: number
  draws: number
  losses: number
  points?: number
  setsWon?: number
  setsLost?: number
  setDifference?: number
  pointsWon?: number
  pointsLost?: number
  pointDifference?: number
}

export interface FinalStandingDto {
  position: number
  teamId: string
  teamName: string
  levelName?: string
}
