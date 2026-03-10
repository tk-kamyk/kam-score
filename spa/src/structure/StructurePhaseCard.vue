<script setup lang="ts">
import { ref, computed } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import CollapsiblePhaseCard from '@/components/CollapsiblePhaseCard.vue'
import StructureGroupItem from '@/structure/StructureGroupItem.vue'
import StructureLevelHeader from '@/structure/StructureLevelHeader.vue'
import type { PhaseDto } from '@/structure/types'
import type { TeamDto } from '@/team/types'
import type { VForm } from 'vuetify/components'

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
const { fieldErrors, handleError, clearErrors, clearFieldError, generalError } = useFormErrors()

const showDeleteDialog = ref(false)
const showAddGroupDialog = ref(false)
const showAutoAssignDialog = ref(false)
const newGroupName = ref('')
const groupFormRef = ref<InstanceType<typeof VForm> | null>(null)

const previousPhaseId = computed(() => {
  const phases = structureStore.structure?.phases ?? []
  const currentOrder = props.phase.order ?? 1
  if (currentOrder <= 1) return undefined
  return phases.find(p => p.order === currentOrder - 1)?.id
})

const isCompleted = computed(() => props.phase.status === 'Completed')
const isActivated = computed(() => props.phase.status !== 'New')
const structureLockReason = computed(() => {
  if (props.phase.status === 'Completed') return 'Reopen the phase first'
  if (props.phase.status === 'InProgress') return 'Delete games first to edit structure'
  return ''
})

const deleteDialogTitleId = computed(() => `delete-phase-title-${props.phase.id}`)
const addGroupDialogTitleId = computed(() => `add-group-title-${props.phase.id}`)
const autoAssignDialogTitleId = computed(() => `auto-assign-title-${props.phase.id}`)

const hasLevels = computed(() => (props.phase.levels?.length ?? 0) > 0)

const groupsByLevel = computed(() => {
  if (!hasLevels.value) return []
  return (props.phase.levels ?? []).map(level => ({
    level,
    groups: (props.phase.groups ?? []).filter(g => g.levelId === level.id),
  }))
})

const groupNameRules = [
  (v: string) => !!v || 'Group name is required.',
  (v: string) => v.length <= 100 || 'Group name must not exceed 100 characters.',
]

function confirmDelete() {
  showDeleteDialog.value = true
}

function handleDelete() {
  showDeleteDialog.value = false
  emit('delete', props.phase.id!)
}

function openAddGroup() {
  newGroupName.value = ''
  clearErrors()
  showAddGroupDialog.value = true
}

async function handleAddGroup() {
  const { valid } = await groupFormRef.value!.validate()
  if (!valid) return

  try {
    await structureStore.addGroup(props.tournamentId, props.phase.id!, { name: newGroupName.value })
    showAddGroupDialog.value = false
    showSuccess('Group added')
    await structureStore.fetchStructure(props.tournamentId)
  } catch (error) {
    if (!handleError(error)) {
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
    if (!handleError(error)) {
      showError('Failed to auto-assign teams')
    }
  }
}
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

    <template #header-actions>
      <div v-if="editing">
        <v-tooltip v-if="isCompleted" text="Reopen the phase first" location="top">
          <template #activator="{ props: tp }">
            <v-btn v-bind="tp" icon="mdi-pencil" variant="text" size="small" aria-disabled="true" :aria-label="'Edit phase ' + phase.name" />
          </template>
        </v-tooltip>
        <v-btn v-else icon="mdi-pencil" variant="text" size="small" :aria-label="'Edit phase ' + phase.name" @click.stop="emit('edit', phase)" />
        <v-tooltip v-if="isActivated" :text="structureLockReason" location="top">
          <template #activator="{ props: tp }">
            <v-btn v-bind="tp" icon="mdi-delete" variant="text" size="small" color="error" aria-disabled="true" :aria-label="'Delete phase ' + phase.name" />
          </template>
        </v-tooltip>
        <v-btn
          v-else
          icon="mdi-delete"
          variant="text"
          size="small"
          color="error"
          :aria-label="'Delete phase ' + phase.name"
          @click.stop="confirmDelete"
        />

        <v-dialog v-model="showDeleteDialog" max-width="400" :aria-labelledby="deleteDialogTitleId">
          <v-card class="pa-2">
            <v-card-title :id="deleteDialogTitleId" class="text-uppercase dialog-title"
              >Delete Phase</v-card-title
            >
            <v-card-text>
              Are you sure you want to delete "{{ phase.name }}"? This will remove all groups and team
              assignments within this phase.
            </v-card-text>
            <v-card-actions>
              <v-spacer />
              <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
              <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
            </v-card-actions>
          </v-card>
        </v-dialog>
      </div>
    </template>

    <v-card-text>
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
      <!-- Groups organized by level -->
      <template v-if="hasLevels">
        <div v-for="{ level, groups } in groupsByLevel" :key="level.id" class="level-section mb-4">
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
            />
          </div>
        </div>
      </template>

      <!-- Flat groups grid (no levels) -->
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
      <v-tooltip v-if="isActivated" :text="structureLockReason" location="top">
        <template #activator="{ props: tp }">
          <v-btn
            v-bind="tp"
            color="primary"
            variant="elevated"
            prepend-icon="mdi-plus"
            aria-disabled="true"
          >
            Add Group
          </v-btn>
        </template>
      </v-tooltip>
      <v-btn
        v-else
        color="primary"
        variant="elevated"
        prepend-icon="mdi-plus"
        @click="openAddGroup"
      >
        Add Group
      </v-btn>
      <v-tooltip v-if="isActivated" :text="structureLockReason" location="top">
        <template #activator="{ props: tp }">
          <v-btn
            v-bind="tp"
            color="primary"
            variant="elevated"
            prepend-icon="mdi-shuffle-variant"
            aria-disabled="true"
          >
            Auto-assign Teams
          </v-btn>
        </template>
      </v-tooltip>
      <v-btn
        v-else
        color="primary"
        variant="elevated"
        prepend-icon="mdi-shuffle-variant"
        @click="clearErrors(); showAutoAssignDialog = true"
      >
        Auto-assign Teams
      </v-btn>
    </template>

    <v-dialog v-model="showAutoAssignDialog" max-width="400" :aria-labelledby="autoAssignDialogTitleId">
      <v-card class="pa-2">
        <v-card-title :id="autoAssignDialogTitleId" class="text-uppercase dialog-title"
          >Auto-assign Teams</v-card-title
        >
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          This will clear existing team assignments and redistribute all tournament teams into the
          groups of "{{ phase.name }}". Continue?
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showAutoAssignDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleAutoAssign">Assign</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showAddGroupDialog" max-width="400" :aria-labelledby="addGroupDialogTitleId">
      <v-card class="pa-2">
        <v-card-title :id="addGroupDialogTitleId" class="text-uppercase dialog-title"
          >Add Group</v-card-title
        >
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          <v-form ref="groupFormRef" @submit.prevent="handleAddGroup">
            <v-text-field
              v-model="newGroupName"
              label="Group Name"
              :rules="groupNameRules"
              :error-messages="fieldErrors('name')"
              @update:model-value="clearFieldError('name')"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showAddGroupDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleAddGroup">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </CollapsiblePhaseCard>
</template>

<style scoped>
.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 12px;
}

.level-section:not(:last-child) {
  padding-bottom: 4px;
  border-bottom: 1px solid var(--ks-border-subtle);
}
</style>
