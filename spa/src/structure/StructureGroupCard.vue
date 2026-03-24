<script setup lang="ts">
import { ref } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import type { GroupDto } from '@/structure/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  phaseId: string
  group: GroupDto
  hasLevels?: boolean
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError, generalError } = useFormErrors()

const showRenameDialog = ref(false)
const showDeleteDialog = ref(false)
const newName = ref('')
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const nameRules = [
  (v: string) => !!v || 'Group name is required.',
  (v: string) => v.length <= 100 || 'Group name must not exceed 100 characters.',
]

function openRename() {
  newName.value = props.group.name
  clearErrors()
  showRenameDialog.value = true
}

async function handleRename() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  try {
    await structureStore.updateGroup(props.tournamentId, props.phaseId, props.group.id!, {
      name: newName.value,
    })
    showRenameDialog.value = false
    showSuccess('Group renamed')
    await structureStore.fetchStructure(props.tournamentId)
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to rename group')
    }
  }
}

async function handleDelete() {
  try {
    await structureStore.deleteGroup(props.tournamentId, props.phaseId, props.group.id!)
    showDeleteDialog.value = false
    showSuccess('Group deleted')
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to delete group')
    }
  }
}
</script>

<template>
  <div class="d-inline">
    <v-btn icon="mdi-pencil" variant="text" size="x-small" :aria-label="'Rename group ' + group.name" @click="openRename" />
    <v-btn
      v-if="!hasLevels"
      icon="mdi-delete"
      variant="text"
      size="x-small"
      color="error"
      :aria-label="'Delete group ' + group.name"
      @click="clearErrors(); showDeleteDialog = true"
    />

    <v-dialog v-model="showRenameDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title"
          >Rename Group</v-card-title
        >
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          <v-form ref="formRef" @submit.prevent="handleRename">
            <v-text-field
              v-model="newName"
              label="Group Name"
              :rules="nameRules"
              :error-messages="fieldErrors('name')"
              @update:model-value="clearFieldError('name')"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showRenameDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleRename">Rename</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title"
          >Delete Group</v-card-title
        >
        <v-card-text>
          <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
            {{ generalError }}
          </v-alert>
          Are you sure you want to delete group "{{ group.name }}"?
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
