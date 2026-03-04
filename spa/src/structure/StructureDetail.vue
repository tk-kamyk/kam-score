<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useStructureStore } from '@/structure/store'
import { useTeamStore } from '@/team/store'
import { useSnackbar } from '@/composables/useSnackbar'
import PhaseCard from '@/structure/PhaseCard.vue'
import PhaseForm from '@/structure/PhaseForm.vue'
import type { PhaseDto } from '@/structure/types'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
}>()

const route = useRoute()
const router = useRouter()
const structureStore = useStructureStore()
const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()

const editing = ref(false)
const showPhaseForm = ref(false)
const editingPhase = ref<PhaseDto | null>(null)

const hasStructure = computed(() => structureStore.structure?.id != null)
const phases = computed(() => structureStore.structure?.phases ?? [])

function parseQuerySet(param: unknown): Set<string> {
  if (!param || typeof param !== 'string') return new Set()
  return new Set(param.split(',').filter(Boolean))
}

const expandedPhases = ref(parseQuerySet(route.query.phase))

watch(expandedPhases, (phases) => {
  const query = { ...route.query }
  if (phases.size > 0) {
    query.phase = [...phases].join(',')
  } else {
    delete query.phase
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
}

onMounted(() => {
  structureStore.fetchStructure(props.tournamentId)
  teamStore.fetchTeams(props.tournamentId)
})

async function handleInitialize() {
  try {
    await structureStore.initializeStructure(props.tournamentId)
    editing.value = true
    showSuccess('Structure initialized')
  } catch {
    showError('Failed to initialize structure')
  }
}

function openAddPhase() {
  editingPhase.value = null
  showPhaseForm.value = true
}

function openEditPhase(phase: PhaseDto) {
  editingPhase.value = phase
  showPhaseForm.value = true
}

async function handlePhaseSaved() {
  showPhaseForm.value = false
  await structureStore.fetchStructure(props.tournamentId)
}

async function handleDeletePhase(phaseId: string) {
  try {
    await structureStore.deletePhase(props.tournamentId, phaseId)
    showSuccess('Phase deleted')
  } catch {
    showError('Failed to delete phase')
  }
}
</script>

<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <h3 class="section-title text-title-small text-md-title-medium">Structure</h3>
      <div v-if="isOwner" class="d-flex ga-2">
        <v-btn
          v-if="!hasStructure"
          color="primary"
          prepend-icon="mdi-cog"
          @click="handleInitialize"
        >
          Initialize Structure
        </v-btn>
        <template v-else>
          <v-btn
            v-if="!editing"
            color="primary"
            prepend-icon="mdi-pencil"
            @click="editing = true"
          >
            Edit
          </v-btn>
          <template v-else>
            <v-btn
              color="primary"
              prepend-icon="mdi-plus"
              @click="openAddPhase"
            >
              Add Phase
            </v-btn>
            <v-btn
              color="primary"
              prepend-icon="mdi-check"
              @click="editing = false"
            >
              Done
            </v-btn>
          </template>
        </template>
      </div>
    </div>

    <v-progress-linear v-if="structureStore.loading" indeterminate color="primary" class="mb-4" />

    <template v-if="hasStructure">
      <div v-if="phases.length > 0" class="phases-list">
        <PhaseCard
          v-for="phase in phases"
          :key="phase.id"
          :phase="phase"
          :tournament-id="tournamentId"
          :editing="editing"
          :expanded="expandedPhases.has(phase.id!)"
          :teams="teamStore.teams"
          @toggle-phase="togglePhase(phase.id!)"
          @edit="openEditPhase"
          @delete="handleDeletePhase"
        />
      </div>

      <v-alert class="mt-4 mb-4" v-else-if="!structureStore.loading" type="info" variant="tonal">
        No phases yet. {{ isOwner ? 'Click Edit to start adding phases.' : '' }}
      </v-alert>
    </template>

    <v-alert class="mt-4 mb-4" v-else-if="!structureStore.loading" type="info" variant="tonal">
      No structure configured.
      {{ isOwner ? 'Click "Initialize Structure" to get started.' : '' }}
    </v-alert>

    <PhaseForm
      v-model="showPhaseForm"
      :tournament-id="tournamentId"
      :phase="editingPhase"
      @saved="handlePhaseSaved"
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
