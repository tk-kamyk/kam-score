<script setup lang="ts">
import { computed } from 'vue'
import type { PhaseDto } from '@/structure/types'
import type { GameDto } from '@/game/types'
import CollapsiblePhaseCard from '@/components/CollapsiblePhaseCard.vue'
import ScheduleGroupGames from '@/game/ScheduleGroupGames.vue'

const props = defineProps<{
  phase: PhaseDto
  games: GameDto[]
  expanded: boolean
  expandedGroups: Set<string>
  isOwner: boolean
  generating: boolean
  completing: boolean
  reopening: boolean
}>()

const emit = defineEmits<{
  'toggle-phase': []
  'toggle-group': [groupId: string]
  generate: []
  delete: []
  complete: []
  reopen: []
  'open-result': [game: GameDto]
}>()

const allGamesCompleted = computed(() =>
  props.games.length > 0 && props.games.every(g => g.status === 'Completed'),
)

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
  <CollapsiblePhaseCard :phase="phase" :expanded="expanded" @toggle="emit('toggle-phase')">
    <v-card-text class="py-0">
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

    <template v-if="isOwner" #actions>
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
      <template v-else>
        <v-btn
          v-if="phase.status === 'InProgress' && allGamesCompleted"
          color="primary"
          variant="elevated"
          prepend-icon="mdi-check-circle-outline"
          :loading="completing"
          @click="emit('complete')"
        >
          Complete Phase
        </v-btn>
        <v-btn
          v-if="phase.status === 'Completed'"
          color="warning"
          variant="elevated"
          prepend-icon="mdi-restart"
          :loading="reopening"
          @click="emit('reopen')"
        >
          Reopen Phase
        </v-btn>
        <v-btn
          color="error"
          variant="elevated"
          prepend-icon="mdi-delete"
          @click="emit('delete')"
        >
          Delete Games
        </v-btn>
      </template>
    </template>
  </CollapsiblePhaseCard>
</template>
