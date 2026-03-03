<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
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

const structureStore = useStructureStore()
const teamStore = useTeamStore()
const { showSuccess, showError } = useSnackbar()

const editing = ref(false)
const showPhaseForm = ref(false)
const editingPhase = ref<PhaseDto | null>(null)

const hasStructure = computed(() => structureStore.structure?.id != null)
const phases = computed(() => structureStore.structure?.phases ?? [])

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
      <h3 class="section-title text-title-medium text-md-title-large">Structure</h3>
      <div v-if="isOwner">
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
            variant="outlined"
            prepend-icon="mdi-pencil"
            @click="editing = true"
          >
            Edit
          </v-btn>
          <v-btn
            v-else
            color="primary"
            variant="outlined"
            prepend-icon="mdi-check"
            @click="editing = false"
          >
            Done
          </v-btn>
        </template>
      </div>
    </div>

    <v-progress-linear v-if="structureStore.loading" indeterminate color="primary" class="mb-4" />

    <template v-if="hasStructure">
      <div v-if="editing" class="text-right">
        <v-btn
          color="primary"
          prepend-icon="mdi-plus"
          class="mb-4"
          @click="openAddPhase"
        >
          Add Phase
        </v-btn>
      </div>

      <div v-if="phases.length > 0" class="phases-list">
        <PhaseCard
          v-for="phase in phases"
          :key="phase.id"
          :phase="phase"
          :tournament-id="tournamentId"
          :editing="editing"
          :teams="teamStore.teams"
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
