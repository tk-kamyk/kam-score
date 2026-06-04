// Station colours — opaque proxies for the physical stations a volunteer is sent to during a
// shift. A station value is an index into this array; null = no station.
//
// IMPORTANT: the length MUST match StationPalette.Count on the backend
// (api/src/KamSquare.KamScore.Domain/Services/StationPalette.cs). The backend validates the
// index range against that count; this array owns the actual colours.
// Ordered so that low indices are maximally distinct from each other — auto-assign and small
// station counts then pick the most visually separable colours first.
export const STATION_COLORS = [
  '#e5534b', // 0 — red
  '#4a8cff', // 1 — blue
  '#56d364', // 2 — green
  '#e3b341', // 3 — yellow
  '#a371f7', // 4 — purple
  '#29b5d4', // 5 — teal
  '#e8873a', // 6 — orange
  '#f778ba', // 7 — pink
] as const

export const STATION_COUNT = STATION_COLORS.length

export function stationColor(index: number | null | undefined): string | undefined {
  if (index == null || index < 0 || index >= STATION_COLORS.length) return undefined
  return STATION_COLORS[index]
}
