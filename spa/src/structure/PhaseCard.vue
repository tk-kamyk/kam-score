<script setup lang="ts">
import { ref } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import { formatPhaseFormat } from '@/structure/types'
import GroupCard from '@/structure/GroupCard.vue'
import TeamAssignmentForm from '@/structure/TeamAssignmentForm.vue'
import type { PhaseDto } from '@/structure/types'
import type { TeamDto } from '@/team/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  phase: PhaseDto
  tournamentId: string
  editing: boolean
  teams: TeamDto[]
}>()

const emit = defineEmits<{
  edit: [phase: PhaseDto]
  delete: [phaseId: string]
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError } = useFormErrors()

const showDeleteDialog = ref(false)
const showAddGroupDialog = ref(false)
const newGroupName = ref('')
const groupFormRef = ref<InstanceType<typeof VForm> | null>(null)

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
</script>

<template>
  <v-card class="phase-card">
    <v-card-title class="d-flex align-center justify-space-between">
      <div>
        <span class="text-h6">{{ phase.name }}</span>
        <v-chip size="small" class="ml-2" color="primary" variant="tonal">
          {{ formatPhaseFormat(phase.format) }}
        </v-chip>
        <v-chip size="x-small" class="ml-1" variant="outlined">
          #{{ phase.order }}
        </v-chip>
      </div>
      <div v-if="editing">
        <v-btn icon="mdi-pencil" variant="text" size="small" @click="emit('edit', phase)" />
        <v-btn
          icon="mdi-delete"
          variant="text"
          size="small"
          color="error"
          @click="confirmDelete"
        />
      </div>
    </v-card-title>

    <v-card-text>
      <div v-if="phase.groups && phase.groups.length > 0" class="groups-grid">
        <v-card
          v-for="group in phase.groups"
          :key="group.id"
          variant="outlined"
          class="group-card"
        >
          <v-card-title class="d-flex align-center justify-space-between py-2">
            <span class="text-subtitle-1 font-weight-medium">Group {{ group.name }}</span>
            <GroupCard
              v-if="editing"
              :tournament-id="tournamentId"
              :phase-id="phase.id!"
              :group="group"
            />
          </v-card-title>
          <v-card-text class="pt-0">
            <TeamAssignmentForm
              :tournament-id="tournamentId"
              :phase-id="phase.id!"
              :group="group"
              :teams="teams"
              :editing="editing"
              :all-groups="phase.groups ?? []"
            />
          </v-card-text>
        </v-card>
      </div>

      <v-alert v-else type="info" variant="tonal" density="compact">
        No groups in this phase.
      </v-alert>

      <v-btn
        v-if="editing"
        variant="tonal"
        size="small"
        prepend-icon="mdi-plus"
        class="mt-3"
        @click="openAddGroup"
      >
        Add Group
      </v-btn>
    </v-card-text>

    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase" style="letter-spacing: 1.5px"
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

    <v-dialog v-model="showAddGroupDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase" style="letter-spacing: 1.5px"
          >Add Group</v-card-title
        >
        <v-card-text>
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
  </v-card>
</template>

<style scoped>
.phase-card {
  border: 1px solid rgba(var(--ks-surface), 0.5);
}

.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 12px;
}

.group-card {
  border-color: rgba(var(--ks-surface), 0.3);
}
</style>
