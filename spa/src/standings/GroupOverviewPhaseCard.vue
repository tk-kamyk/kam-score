<script setup lang="ts">
import { computed } from 'vue'
import { formatPhaseFormat } from '@/structure/types'
import type { PhaseDto } from '@/structure/types'
import type { GameDto } from '@/game/types'
import type { StandingDto } from '@/standings/types'
import GroupStandings from '@/standings/GroupStandings.vue'
import GroupOverviewGames from '@/standings/GroupOverviewGames.vue'

const props = defineProps<{
  phase: PhaseDto
  games: GameDto[]
  standings: StandingDto[]
  expanded: boolean
  selectedGroupId: string | null
}>()

const emit = defineEmits<{
  'toggle-phase': []
  'select-group': [groupId: string]
  'open-result': [game: GameDto]
}>()

const selectedGroupGames = computed(() => {
  if (!props.selectedGroupId) return []
  return props.games
    .filter(g => g.groupId === props.selectedGroupId)
    .sort((a, b) => (a.round ?? 0) - (b.round ?? 0))
})
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
      </div>
    </v-card-title>

    <v-card-text v-if="expanded" class="px-8 pb-8">
      <div v-if="phase.groups && phase.groups.length > 0" class="mb-4">
        <v-chip-group
          :model-value="selectedGroupId"
          @update:model-value="(val: unknown) => { if (typeof val === 'string') emit('select-group', val) }"
          selected-class="text-primary"
          mandatory
        >
          <v-chip
            v-for="group in phase.groups"
            :key="group.id"
            :value="group.id"
            variant="outlined"
            filter
          >
            Group {{ group.name }}
          </v-chip>
        </v-chip-group>
      </div>

      <template v-if="selectedGroupId">
        <div class="mb-8">
          <h4 class="text-title-small text-md-title-medium mb-2 mb-md-4 text-center text-uppercase">Standings</h4>
          <GroupStandings
            :standings="standings"
            :phase-format="phase.format"
          />
        </div>

        <div v-if="selectedGroupGames.length > 0">
          <h4 class="text-title-small text-md-title-medium mb-2 mb-md-4 text-center text-uppercase">Games</h4>
          <GroupOverviewGames
            :games="selectedGroupGames"
            @open-result="(game) => emit('open-result', game)"
          />
        </div>

        <v-alert
          v-else
          class="mt-4"
          type="info"
          variant="tonal"
          density="compact"
        >
          No games generated for this group yet.
        </v-alert>
      </template>
    </v-card-text>
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
