<script setup lang="ts">
import { ref, computed } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import CollapsiblePhaseCard from '@/components/CollapsiblePhaseCard.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import AddGroupDialog from '@/structure/AddGroupDialog.vue'
import StructureGroupItem from '@/structure/StructureGroupItem.vue'
import StructureLevelHeader from '@/structure/StructureLevelHeader.vue'
import type { PhaseDto } from '@/structure/types'
import type { TeamDto } from '@/team/types'

const props = defineProps<{
  phase: PhaseDto
  tournamentId: string
  editing: boolean
  expanded: boolean
  teams: TeamDto[]
}>()

const emit = defineEmits<{
  'toggle-phase': []
  edit: [phase: PhaseDto]
  delete: [phaseId: string]
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()

const showDeleteDialog = ref(false)
const showAddGroupDialog = ref(false)
const showAutoAssignDialog = ref(false)
const addGroupDialog = ref<InstanceType<typeof AddGroupDialog> | null>(null)
const autoAssignDialog = ref<InstanceType<typeof ConfirmDialog> | null>(null)

const previousPhaseId = computed(() => {
  const phases = structureStore.structure?.phases ?? []
  const currentOrder = props.phase.order ?? 1
  if (currentOrder <= 1) return undefined
  return phases.find((p) => p.order === currentOrder - 1)?.id
})

const isCompleted = computed(() => props.phase.status === 'Completed')
const isActivated = computed(() => props.phase.status !== 'New')
const isCustom = computed(() => props.phase.format === 'Custom')
const structureLockReason = computed(() => {
  if (props.phase.status === 'Completed') return 'Reopen the phase first'
  if (props.phase.status === 'Scheduled' || props.phase.status === 'InProgress')
    return isCustom.value
      ? 'Reset the phase first to edit structure'
      : 'Delete games first to edit structure'
  return ''
})

const addGroupDialogTitleId = computed(() => `add-group-title-${props.phase.id}`)
const hasLevels = computed(() => (props.phase.levels?.length ?? 0) > 0)

const groupsByLevel = computed(() => {
  if (!hasLevels.value) return []
  return (props.phase.levels ?? []).map((level) => ({
    level,
    groups: (props.phase.groups ?? []).filter((g) => g.levelId === level.id),
  }))
})

function handleDelete() {
  showDeleteDialog.value = false
  emit('delete', props.phase.id!)
}

async function handleAddGroup(name: string) {
  try {
    await structureStore.addGroup(props.tournamentId, props.phase.id!, { name })
    showAddGroupDialog.value = false
    showSuccess('Group added')
    await structureStore.fetchStructure(props.tournamentId)
  } catch (error) {
    if (!addGroupDialog.value?.handleError(error)) {
      showError('Failed to add group')
    }
  }
}

async function handleAutoAssign() {
  try {
    await structureStore.autoAssignTeams(props.tournamentId, props.phase.id!)
    showAutoAssignDialog.value = false
    showSuccess('Teams auto-assigned')
  } catch (error) {
    if (!autoAssignDialog.value?.handleError(error)) {
      showError('Failed to auto-assign teams')
    }
  }
}
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

    <template #header-actions>
      <div v-if="editing">
        <v-btn
          icon="mdi-pencil"
          variant="text"
          size="small"
          :aria-label="'Edit phase ' + phase.name"
          :disabled="isCompleted"
          @click.stop="emit('edit', phase)"
        />
        <v-btn
          icon="mdi-delete"
          variant="text"
          size="small"
          color="error"
          :aria-label="'Delete phase ' + phase.name"
          :disabled="isActivated"
          @click.stop="showDeleteDialog = true"
        />
      </div>
    </template>

    <v-card-text class="px-lg-8 pb-lg-8">
      <v-alert
        v-if="editing && isActivated"
        type="warning"
        variant="tonal"
        density="compact"
        class="mb-3"
        prepend-icon="mdi-lock"
      >
        {{ structureLockReason }}
      </v-alert>
      <div v-if="hasLevels" class="levels-row">
        <div v-for="{ level, groups } in groupsByLevel" :key="level.id" class="level-section">
          <StructureLevelHeader
            :tournament-id="tournamentId"
            :phase-id="phase.id!"
            :level="level"
            :editing="editing && !isActivated"
          />
          <div v-if="groups.length > 0" class="groups-grid">
            <StructureGroupItem
              v-for="group in groups"
              :key="group.id"
              :tournament-id="tournamentId"
              :phase-id="phase.id!"
              :group="group"
              :teams="teams"
              :editing="editing && !isActivated"
              :all-groups="phase.groups ?? []"
              :phase-order="phase.order ?? 1"
              :previous-phase-id="previousPhaseId"
              :single-group="groups.length === 1"
              :has-levels="true"
            />
          </div>
        </div>
      </div>

      <div v-else-if="phase.groups && phase.groups.length > 0" class="groups-grid">
        <StructureGroupItem
          v-for="group in phase.groups"
          :key="group.id"
          :tournament-id="tournamentId"
          :phase-id="phase.id!"
          :group="group"
          :teams="teams"
          :editing="editing && !isActivated"
          :all-groups="phase.groups ?? []"
          :phase-order="phase.order ?? 1"
          :previous-phase-id="previousPhaseId"
        />
      </div>

      <v-alert v-else class="mt-4 mb-4" type="info" variant="tonal" density="compact">
        No groups in this phase.
      </v-alert>
    </v-card-text>

    <template v-if="editing" #actions>
      <v-btn
        v-if="!hasLevels"
        color="primary"
        variant="elevated"
        prepend-icon="mdi-plus"
        :disabled="isActivated"
        @click="showAddGroupDialog = true"
      >
        Add Group
      </v-btn>
      <v-btn
        color="primary"
        variant="elevated"
        prepend-icon="mdi-shuffle-variant"
        :disabled="isActivated"
        @click="showAutoAssignDialog = true"
      >
        Auto-assign Teams
      </v-btn>
    </template>

    <ConfirmDialog
      v-model="showDeleteDialog"
      title="Delete Phase"
      :message="`Are you sure you want to delete &quot;${phase.name}&quot;? This will remove all groups and team assignments within this phase.`"
      confirm-label="Delete"
      confirm-color="error"
      @confirm="handleDelete"
    />
    <ConfirmDialog
      ref="autoAssignDialog"
      v-model="showAutoAssignDialog"
      title="Auto-assign Teams"
      :message="`This will clear existing team assignments and redistribute all tournament teams into the groups of &quot;${phase.name}&quot;. Continue?`"
      confirm-label="Assign"
      @confirm="handleAutoAssign"
    />
    <AddGroupDialog
      ref="addGroupDialog"
      v-model="showAddGroupDialog"
      :title-id="addGroupDialogTitleId"
      @add="handleAddGroup"
    />
  </CollapsiblePhaseCard>
</template>

<style scoped>
.levels-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
}

.level-section {
  flex: 1 1 250px;
}

.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 12px;
}
</style>
