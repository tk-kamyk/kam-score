export interface VolunteerDto {
  id?: string
  name: string
  contact?: string | null
  teamId?: string | null
  assignments?: VolunteerShiftAssignment[]
}

export interface VolunteerShiftAssignment {
  shiftGroup: string
  shiftTime?: string | null
  station?: number | null
}

export interface ShiftGroupDto {
  name: string
  isSpecial: boolean
  shifts: ShiftSlotDto[]
}

export interface ShiftSlotDto {
  shiftTime?: string | null
  volunteers: ShiftVolunteerDto[]
}

export interface ShiftVolunteerDto {
  volunteerId: string
  name: string
  available: boolean
  station?: number | null
}

export interface VolunteerAvailabilityDto {
  volunteerId: string
  name: string
  shiftCount: number
  available: boolean
  playsBefore: boolean
  playsAfter: boolean
  assigned: boolean
  station?: number | null
}
