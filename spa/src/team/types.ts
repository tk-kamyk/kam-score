export interface TeamDto {
  id?: string
  name: string
  level: number
  email?: string | null
  phone?: string | null
  isPlaceholder?: boolean
  sourcePhaseId?: string
  seed?: number
  resolvedTeamId?: string
}
