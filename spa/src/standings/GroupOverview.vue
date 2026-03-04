<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGameStore } from '@/game/store'
import { useStructureStore } from '@/structure/store'
import { useStandingsStore } from '@/standings/store'
import type { GameDto } from '@/game/types'
import GroupOverviewPhaseCard from '@/standings/GroupOverviewPhaseCard.vue'
import GameResultDialog from '@/game/GameResultDialog.vue'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const structureStore = useStructureStore()
const standingsStore = useStandingsStore()

function parseQuerySet(param: unknown): Set<string> {
  if (!param || typeof param !== 'string') return new Set()
  return new Set(param.split(',').filter(Boolean))
}

function parseGroupSelections(param: unknown): Map<string, string> {
  if (!param || typeof param !== 'string') return new Map()
  const map = new Map<string, string>()
  for (const entry of param.split(',').filter(Boolean)) {
    const [phaseId, groupId] = entry.split(':')
    if (phaseId && groupId) map.set(phaseId, groupId)
  }
  return map
}

const expandedPhases = ref(parseQuerySet(route.query.phase))
const selectedGroups = ref(parseGroupSelections(route.query.group))

watch(expandedPhases, (phases) => {
  const query = { ...route.query }
  if (phases.size > 0) {
    query.phase = [...phases].join(',')
  } else {
    delete query.phase
  }
  router.replace({ query })
}, { deep: true })

watch(selectedGroups, (groups) => {
  const query = { ...route.query }
  if (groups.size > 0) {
    query.group = [...groups.entries()].map(([p, g]) => `${p}:${g}`).join(',')
  } else {
    delete query.group
  }
  router.replace({ query })
}, { deep: true })

function togglePhase(phaseId: string) {
  const newSet = new Set(expandedPhases.value)
  if (newSet.has(phaseId)) {
    newSet.delete(phaseId)
  } else {
    newSet.add(phaseId)
  }
  expandedPhases.value = newSet

  // Fetch standings for newly expanded phase
  if (newSet.has(phaseId)) {
    if (!selectedGroups.value.has(phaseId)) {
      const phase = phases.value.find(p => p.id === phaseId)
      if (phase?.groups?.[0]?.id) {
        selectGroup(phaseId, phase.groups[0].id)
      }
    } else {
      const groupId = selectedGroups.value.get(phaseId)!
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  }
}

function selectGroup(phaseId: string, groupId: string) {
  const newMap = new Map(selectedGroups.value)
  newMap.set(phaseId, groupId)
  selectedGroups.value = newMap
  standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
}

const showResultDialog = ref(false)
const selectedGame = ref<GameDto | null>(null)

watch(showResultDialog, (open) => {
  if (!open) {
    for (const [phaseId, groupId] of selectedGroups.value) {
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  }
})

watch(() => props.active, async (isActive) => {
  if (!isActive) return
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])
  for (const [phaseId, groupId] of selectedGroups.value) {
    standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
  }
})

function openResultDialog(game: GameDto) {
  selectedGame.value = game
  showResultDialog.value = true
}

const phases = computed(() => structureStore.structure?.phases ?? [])

const gamesByPhase = computed(() => {
  const map: Record<string, GameDto[]> = {}
  for (const game of gameStore.games) {
    const key = game.phaseId ?? ''
    if (!map[key]) map[key] = []
    map[key].push(game)
  }
  return map
})

function phaseGames(phaseId: string): GameDto[] {
  return gamesByPhase.value[phaseId] ?? []
}

onMounted(async () => {
  await Promise.all([
    structureStore.fetchStructure(props.tournamentId),
    gameStore.fetchGames(props.tournamentId),
  ])

  // Auto-select first group for expanded phases if not already selected
  for (const phaseId of expandedPhases.value) {
    if (!selectedGroups.value.has(phaseId)) {
      const phase = phases.value.find(p => p.id === phaseId)
      if (phase?.groups?.[0]?.id) {
        selectGroup(phaseId, phase.groups[0].id)
      }
    } else {
      const groupId = selectedGroups.value.get(phaseId)!
      standingsStore.fetchStandings(props.tournamentId, phaseId, groupId)
    }
  }
})
</script>

<template>
  <div>
    <div class="mb-6">
      <h3 class="section-title text-title-small text-md-title-medium">Groups</h3>
    </div>

    <v-progress-linear v-if="gameStore.loading" indeterminate color="primary" class="mb-4" />

    <v-alert v-if="phases.length === 0 && !structureStore.loading" class="mt-4 mb-4" type="info" variant="tonal">
      No phases defined yet. Set up the tournament structure first.
    </v-alert>

    <div class="phases-list">
      <GroupOverviewPhaseCard
        v-for="phase in phases"
        :key="phase.id"
        :phase="phase"
        :games="phaseGames(phase.id!)"
        :standings="standingsStore.getStandings(phase.id!, selectedGroups.get(phase.id!) ?? '')"
        :expanded="expandedPhases.has(phase.id!)"
        :selected-group-id="selectedGroups.get(phase.id!) ?? null"
        @toggle-phase="togglePhase(phase.id!)"
        @select-group="(groupId) => selectGroup(phase.id!, groupId)"
        @open-result="openResultDialog"
      />
    </div>

    <GameResultDialog
      v-if="selectedGame"
      v-model="showResultDialog"
      :game="selectedGame"
      :tournament-id="tournamentId"
      :is-owner="isOwner"
    />
  </div>
</template>

<style scoped>
.phases-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
