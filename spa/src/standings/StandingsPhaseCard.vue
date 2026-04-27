<script setup lang="ts">
import { computed } from 'vue'
import type { PhaseDto } from '@/structure/types'
import type { GameDto } from '@/game/types'
import type { StandingDto } from '@/standings/types'
import { useTeamStore } from '@/team/store'
import CollapsiblePhaseCard from '@/components/CollapsiblePhaseCard.vue'
import PhaseGroupTabs from '@/components/PhaseGroupTabs.vue'
import StandingsGroup from '@/standings/StandingsGroup.vue'
import StandingsGroupManualEdit from '@/standings/StandingsGroupManualEdit.vue'
import StandingsGames from '@/standings/StandingsGames.vue'

const props = defineProps<{
  phase: PhaseDto
  games: GameDto[]
  standings: StandingDto[]
  expanded: boolean
  selectedGroupId: string | null
  isOwner: boolean
  savingManualStandings?: boolean
}>()

const emit = defineEmits<{
  'toggle-phase': []
  'select-group': [groupId: string]
  'open-result': [game: GameDto]
  'save-manual-standings': [payload: { groupId: string; orderedTeamIds: string[] }]
}>()

const teamStore = useTeamStore()

const isCustomFormat = computed(() => props.phase.format === 'Custom')

const canEditManualStandings = computed(
  () => isCustomFormat.value && props.isOwner && props.phase.status === 'InProgress',
)

const selectedGroup = computed(() => {
  if (!props.selectedGroupId) return null
  return (props.phase.groups ?? []).find((g) => g.id === props.selectedGroupId) ?? null
})

const teamsInSelectedGroup = computed(() => {
  const group = selectedGroup.value
  if (!group) return []
  const byId = new Map(teamStore.teams.map((t) => [t.id, t]))
  return (group.teamIds ?? []).map((id) => {
    const team = byId.get(id)
    return { id, name: team?.name ?? id }
  })
})

const manualInitialOrder = computed(() => props.standings.map((s) => s.teamId))

function handleManualSave(orderedTeamIds: string[]) {
  if (!props.selectedGroupId) return
  emit('save-manual-standings', {
    groupId: props.selectedGroupId,
    orderedTeamIds,
  })
}

const selectedGroupGames = computed(() => {
  if (!props.selectedGroupId) return []
  return props.games
    .filter((g) => g.groupId === props.selectedGroupId)
    .sort((a, b) => (a.round ?? 0) - (b.round ?? 0))
})

const numberOfGroupsInScope = computed(() => {
  if (!props.selectedGroupId) return 0
  const groups = props.phase.groups ?? []
  const selectedGroup = groups.find((g) => g.id === props.selectedGroupId)
  if (selectedGroup?.levelId) {
    return groups.filter((g) => g.levelId === selectedGroup.levelId).length
  }
  return groups.length
})
</script>

<template>
  <CollapsiblePhaseCard :phase="phase" :expanded="expanded" @toggle="emit('toggle-phase')">
    <template #chips>
      <v-chip
        v-if="phase.groupWinners"
        size="small"
        color="success"
        variant="tonal"
        prepend-icon="mdi-arrow-up"
      >
        Top {{ phase.groupWinners }}
      </v-chip>
      <v-chip
        v-if="phase.totalTeamsProceeding"
        size="small"
        color="info"
        variant="tonal"
        prepend-icon="mdi-arrow-up"
      >
        Total {{ phase.totalTeamsProceeding }}
      </v-chip>
    </template>

    <v-card-text class="px-lg-8 pb-lg-8">
      <PhaseGroupTabs
        :groups="phase.groups ?? []"
        :levels="phase.levels ?? []"
        :selected-group-id="selectedGroupId"
        @select-group="(groupId) => emit('select-group', groupId)"
      />

      <template v-if="selectedGroupId">
        <div class="mb-8">
          <h4 class="text-title-small text-md-title-medium mb-2 mb-md-4 text-center text-uppercase">
            Standings
          </h4>
          <StandingsGroupManualEdit
            v-if="isCustomFormat"
            :teams="teamsInSelectedGroup"
            :initial-order="manualInitialOrder"
            :editable="canEditManualStandings"
            :saving="savingManualStandings"
            @save="handleManualSave"
          />
          <StandingsGroup
            v-else
            :standings="standings"
            :phase-format="phase.format"
            :group-winners="phase.groupWinners"
            :total-teams-proceeding="phase.totalTeamsProceeding"
            :number-of-groups="numberOfGroupsInScope"
          />
        </div>

        <template v-if="!isCustomFormat">
          <div v-if="selectedGroupGames.length > 0">
            <h4
              class="text-title-small text-md-title-medium mb-2 mb-md-4 text-center text-uppercase"
            >
              Games
            </h4>
            <StandingsGames
              :games="selectedGroupGames"
              @open-result="(game) => emit('open-result', game)"
            />
          </div>

          <v-alert v-else class="mt-4" type="info" variant="tonal" density="compact">
            No games generated for this group yet.
          </v-alert>
        </template>
      </template>
    </v-card-text>
  </CollapsiblePhaseCard>
</template>
