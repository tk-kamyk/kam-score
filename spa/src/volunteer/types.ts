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
}

export interface VolunteerAvailabilityDto {
  volunteerId: string
  name: string
  shiftCount: number
  available: boolean
  playsBefore: boolean
  playsAfter: boolean
  assigned: boolean
}
