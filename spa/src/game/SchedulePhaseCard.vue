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

const hasLevels = computed(() => (props.phase.levels?.length ?? 0) > 0)

const groupsByLevel = computed(() => {
  if (!hasLevels.value) return []
  return (props.phase.levels ?? []).map(level => ({
    level,
    groupIds: (props.phase.groups ?? []).filter(g => g.levelId === level.id).map(g => g.id!),
  }))
})

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
        <template v-if="hasLevels">
          <div v-for="{ level, groupIds } in groupsByLevel" :key="level.id" class="level-section">
            <h3 class="text-subtitle-1 font-weight-bold mb-1 mt-2">{{ level.name }}</h3>
            <template v-for="(groupGames, groupId) in gamesByGroup(games)" :key="groupId">
              <ScheduleGroupGames
                v-if="groupIds.includes(groupId as string)"
                :phase-id="phase.id!"
                :group-id="groupId as string"
                :group-name="groupName(groupId as string)"
                :games="groupGames"
                :expanded="expandedGroups.has(`${phase.id}:${groupId}`)"
                @toggle="emit('toggle-group', groupId as string)"
                @open-result="(game) => emit('open-result', game)"
              />
            </template>
          </div>
        </template>
        <template v-else>
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
          v-if="phase.status !== 'Completed'"
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

<style scoped>
.level-section:not(:last-child) {
  padding-bottom: 4px;
  border-bottom: 1px solid var(--ks-border-subtle);
}
</style>
