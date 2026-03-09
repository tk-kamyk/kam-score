import type { GameConditionsDto } from '@/tournament/types'

export function parsePointsPerSet(text: string): number[] | undefined {
  const points = text
    .split(',')
    .map(s => parseInt(s.trim()))
    .filter(n => !isNaN(n))
  return points.length > 0 ? points : undefined
}

export function formatPointsPerSet(points?: number[]): string {
  return points?.join(', ') ?? ''
}

export function buildGameConditions(
  enabled: boolean,
  bestOfSets: number | undefined,
  pointsPerSetText: string,
): GameConditionsDto | undefined {
  if (!enabled) return undefined
  return {
    bestOfSets,
    pointsPerSet: parsePointsPerSet(pointsPerSetText),
  }
}
