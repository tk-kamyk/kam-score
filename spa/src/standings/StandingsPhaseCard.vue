<script setup lang="ts">
import { computed } from 'vue'
import type { PhaseDto } from '@/structure/types'
import type { GameDto } from '@/game/types'
import type { StandingDto } from '@/standings/types'
import CollapsiblePhaseCard from '@/components/CollapsiblePhaseCard.vue'
import StandingsGroup from '@/standings/StandingsGroup.vue'
import StandingsGames from '@/standings/StandingsGames.vue'

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

const hasLevels = computed(() => (props.phase.levels?.length ?? 0) > 0)

const groupsByLevel = computed(() => {
  if (!hasLevels.value) return []
  return (props.phase.levels ?? []).map(level => ({
    level,
    groups: (props.phase.groups ?? []).filter(g => g.levelId === level.id),
  }))
})

const selectedGroupGames = computed(() => {
  if (!props.selectedGroupId) return []
  return props.games
    .filter(g => g.groupId === props.selectedGroupId)
    .sort((a, b) => (a.round ?? 0) - (b.round ?? 0))
})
</script>

<template>
  <CollapsiblePhaseCard :phase="phase" :expanded="expanded" @toggle="emit('toggle-phase')">
    <template #chips>
      <v-chip v-if="phase.groupWinners" size="small" color="success" variant="tonal" prepend-icon="mdi-arrow-up">
        Top {{ phase.groupWinners }}
      </v-chip>
      <v-chip v-if="phase.totalTeamsProceeding" size="small" color="info" variant="tonal" prepend-icon="mdi-arrow-up">
        Total {{ phase.totalTeamsProceeding }}
      </v-chip>
    </template>

    <v-card-text class="px-8 pb-8">
      <div v-if="phase.groups && phase.groups.length > 0" class="mb-4">
        <v-chip-group
          :model-value="selectedGroupId"
          @update:model-value="(val: unknown) => { if (typeof val === 'string') emit('select-group', val) }"
          selected-class="text-primary"
          mandatory
          aria-label="Select group"
        >
          <template v-if="hasLevels">
            <template v-for="{ level, groups } in groupsByLevel" :key="level.id">
              <div class="level-chip-section">
                <div class="text-subtitle-2 font-weight-bold mb-1">{{ level.name }}</div>
                <div class="d-flex flex-wrap ga-1">
                  <v-chip
                    v-for="group in groups"
                    :key="group.id"
                    :value="group.id"
                    variant="outlined"
                    filter
                  >
                    Group {{ group.name }}
                  </v-chip>
                </div>
              </div>
            </template>
          </template>
          <template v-else>
            <v-chip
              v-for="group in phase.groups"
              :key="group.id"
              :value="group.id"
              variant="outlined"
              filter
            >
              Group {{ group.name }}
            </v-chip>
          </template>
        </v-chip-group>
      </div>

      <template v-if="selectedGroupId">
        <div class="mb-8">
          <h4 class="text-title-small text-md-title-medium mb-2 mb-md-4 text-center text-uppercase">Standings</h4>
          <StandingsGroup
            :standings="standings"
            :phase-format="phase.format"
          />
        </div>

        <div v-if="selectedGroupGames.length > 0">
          <h4 class="text-title-small text-md-title-medium mb-2 mb-md-4 text-center text-uppercase">Games</h4>
          <StandingsGames
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
  </CollapsiblePhaseCard>
</template>

<style scoped>
.level-chip-section:not(:last-child) {
  margin-bottom: 8px;
}
</style>
