<script setup lang="ts">
import { computed } from 'vue'
import type { PhaseDto } from '@/structure/types'
import type { GameDto } from '@/game/types'
import CollapsiblePhaseCard from '@/components/CollapsiblePhaseCard.vue'
import PhaseGroupTabs from '@/components/PhaseGroupTabs.vue'
import ScheduleGroupGames from '@/game/ScheduleGroupGames.vue'

const props = defineProps<{
  phase: PhaseDto
  games: GameDto[]
  expanded: boolean
  selectedGroupId: string | null
  isOwner: boolean
  generating: boolean
  completing: boolean
  reopening: boolean
}>()

const emit = defineEmits<{
  'toggle-phase': []
  'select-group': [groupId: string]
  generate: []
  delete: []
  complete: []
  reopen: []
  'open-result': [game: GameDto]
}>()

const allGamesCompleted = computed(() =>
  props.games.length > 0 && props.games.every(g => g.status === 'Completed'),
)

const selectedGroupGames = computed(() => {
  if (!props.selectedGroupId) return []
  return props.games.filter(g => g.groupId === props.selectedGroupId)
})

const selectedGroupName = computed(() => {
  if (!props.selectedGroupId) return ''
  const group = props.phase.groups?.find(g => g.id === props.selectedGroupId)
  return group?.name ?? ''
})
</script>

<template>
  <CollapsiblePhaseCard :phase="phase" :expanded="expanded" @toggle="emit('toggle-phase')">
    <v-card-text class="px-lg-8 pb-lg-8">
      <template v-if="games.length > 0">
        <PhaseGroupTabs
          :groups="phase.groups ?? []"
          :levels="phase.levels ?? []"
          :selected-group-id="selectedGroupId"
          @select-group="(groupId) => emit('select-group', groupId)"
        />

        <ScheduleGroupGames
          v-if="selectedGroupId"
          :phase-id="phase.id!"
          :group-id="selectedGroupId"
          :group-name="selectedGroupName"
          :games="selectedGroupGames"
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
