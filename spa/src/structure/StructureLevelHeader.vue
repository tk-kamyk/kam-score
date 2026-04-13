<script setup lang="ts">
import { ref, nextTick } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import type { LevelDto } from '@/structure/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  phaseId: string
  level: LevelDto
  editing: boolean
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError, generalError } = useFormErrors()

const showRenameDialog = ref(false)
const newName = ref('')
const formRef = ref<InstanceType<typeof VForm> | null>(null)
const renameBtnRef = ref<{ $el?: HTMLElement } | null>(null)

const dialogTitleId = `rename-level-title-${props.level.id}`

const nameRules = [
  (v: string) => !!v || 'Level name is required.',
  (v: string) => v.length <= 100 || 'Level name must not exceed 100 characters.',
]

function openRename() {
  newName.value = props.level.name
  clearErrors()
  showRenameDialog.value = true
}

async function handleRename() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  try {
    await structureStore.updateLevel(props.tournamentId, props.phaseId, props.level.id!, {
      name: newName.value,
    })
    showRenameDialog.value = false
    showSuccess('Level renamed')
    await nextTick()
    renameBtnRef.value?.$el?.focus()
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to rename level')
    }
  }
}
</script>

<template>
  <div class="level-header d-flex align-center ga-2 mb-2">
    <h3 class="text-subtitle-1 font-weight-bold">{{ level.name }}</h3>
    <v-btn
      v-if="editing"
      ref="renameBtnRef"
      icon="mdi-pencil"
      variant="text"
      size="x-small"
      :aria-label="'Rename level ' + level.name"
      @click="openRename"
    />
  </div>

  <v-dialog v-model="showRenameDialog" max-width="400" :aria-labelledby="dialogTitleId">
    <v-card class="pa-2">
      <v-card-title :id="dialogTitleId" class="text-uppercase dialog-title"
        >Rename Level</v-card-title
      >
      <v-card-text>
        <v-alert
          v-if="generalError"
          type="error"
          variant="tonal"
          density="compact"
          closable
          role="alert"
          class="mb-3"
          @click:close="clearErrors()"
        >
          {{ generalError }}
        </v-alert>
        <v-form ref="formRef" @submit.prevent="handleRename">
          <v-text-field
            v-model="newName"
            label="Level Name"
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
</template>
