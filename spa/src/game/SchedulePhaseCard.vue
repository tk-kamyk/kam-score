<script setup lang="ts">
import { formatPhaseFormat } from '@/structure/types'
import type { PhaseDto } from '@/structure/types'
import type { GameDto } from '@/game/types'
import ScheduleGroupGames from '@/game/ScheduleGroupGames.vue'

const props = defineProps<{
  phase: PhaseDto
  games: GameDto[]
  expanded: boolean
  expandedGroups: Set<string>
  isOwner: boolean
  generating: boolean
}>()

const emit = defineEmits<{
  'toggle-phase': []
  'toggle-group': [groupId: string]
  generate: []
  delete: []
  'open-result': [game: GameDto]
}>()

function gamesByGroup(games: GameDto[]): Record<string, GameDto[]> {
  const map: Record<string, GameDto[]> = {}
  for (const game of games) {
    const key = game.groupId ?? ''
    if (!map[key]) map[key] = []
    map[key].push(game)
  }
  return map
}

function groupName(groupId: string): string {
  const group = props.phase.groups?.find(g => g.id === groupId)
  return group?.name ?? groupId
}
</script>

<template>
  <v-card class="phase-card">
    <v-card-title class="d-flex align-center justify-space-between phase-header" @click="emit('toggle-phase')">
      <div class="d-flex align-center flex-wrap ga-1">
        <v-icon
          :icon="expanded ? 'mdi-chevron-down' : 'mdi-chevron-right'"
          size="small"
          class="mr-1"
        />
        <span class="text-title-medium text-sm-headline-small">{{ phase.name }}</span>
        <v-chip size="small" color="primary" variant="tonal" class="ml-4">
          {{ formatPhaseFormat(phase.format) }}
        </v-chip>
        <v-chip v-if="phase.startTime" size="small" color="warning" variant="tonal">
          Starts {{ phase.startTime }}
        </v-chip>
      </div>
    </v-card-title>

    <v-card-text v-if="expanded" class="py-0">
      <template v-if="games.length > 0">
        <ScheduleGroupGames
          v-for="(groupGames, groupId) in gamesByGroup(games)"
          :key="groupId"
          :phase-id="phase.id!"
          :group-id="groupId as string"
          :group-name="groupName(groupId as string)"
          :games="groupGames"
          :expanded="expandedGroups.has(`${phase.id}:${groupId}`)"
          @toggle="emit('toggle-group', groupId as string)"
          @open-result="(game) => emit('open-result', game)"
        />
      </template>

      <v-alert
        v-else
        class="mt-4 mb-4"
        type="info"
        variant="tonal"
        density="compact"
      >
        No games generated for this phase yet.
      </v-alert>
    </v-card-text>

    <v-card-actions v-if="isOwner" class="justify-end pa-4">
      <v-btn
        v-if="games.length === 0"
        color="primary"
        variant="elevated"
        prepend-icon="mdi-calendar-clock"
        :loading="generating"
        @click="emit('generate')"
      >
        Generate &amp; Schedule
      </v-btn>
      <v-btn
        v-else
        color="error"
        variant="elevated"
        prepend-icon="mdi-delete"
        @click="emit('delete')"
      >
        Delete Games
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<style scoped>
.phase-card {
  border: 1px solid var(--ks-border);
}

.phase-header {
  cursor: pointer;
}
</style>
