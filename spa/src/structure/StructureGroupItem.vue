<script setup lang="ts">
import StructureGroupCard from '@/structure/StructureGroupCard.vue'
import TeamAssignmentForm from '@/structure/TeamAssignmentForm.vue'
import type { GroupDto } from '@/structure/types'
import type { TeamDto } from '@/team/types'

defineProps<{
  tournamentId: string
  phaseId: string
  group: GroupDto
  teams: TeamDto[]
  editing: boolean
  allGroups: GroupDto[]
  phaseOrder: number
  previousPhaseId?: string
}>()
</script>

<template>
  <v-card variant="outlined" class="group-card">
    <v-card-title class="d-flex align-center justify-space-between py-2">
      <span class="text-title-medium font-weight-medium">Group {{ group.name }}</span>
      <StructureGroupCard
        v-if="editing"
        :tournament-id="tournamentId"
        :phase-id="phaseId"
        :group="group"
      />
    </v-card-title>
    <v-card-text class="pt-0">
      <TeamAssignmentForm
        :tournament-id="tournamentId"
        :phase-id="phaseId"
        :group="group"
        :teams="teams"
        :editing="editing"
        :all-groups="allGroups"
        :phase-order="phaseOrder"
        :previous-phase-id="previousPhaseId"
      />
    </v-card-text>
  </v-card>
</template>

<style scoped>
.group-card {
  border-color: var(--ks-border-subtle);
  background-color: rgb(var(--v-theme-surface-bright));
}
</style>
