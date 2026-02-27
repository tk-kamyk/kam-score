<script setup lang="ts">
import { ref, watch } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import { PHASE_FORMATS } from '@/structure/types'
import type { PhaseDto } from '@/structure/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  phase: PhaseDto | null
}>()

const model = defineModel<boolean>()

const emit = defineEmits<{
  saved: []
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError } = useFormErrors()
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const form = ref({
  name: '',
  format: 'RoundRobin',
  numberOfGroups: 2,
  groupWinners: null as number | null,
  totalTeamsProceeding: null as number | null,
  startTime: '',
})

const nameRules = [
  (v: string) => !!v || 'Phase name is required.',
  (v: string) => v.length <= 200 || 'Phase name must not exceed 200 characters.',
]

const groupsRules = [
  (v: number) => v >= 1 || 'At least 1 group is required.',
]

watch(model, (open) => {
  if (open) {
    clearErrors()
    if (props.phase) {
      form.value = {
        name: props.phase.name,
        format: props.phase.format,
        numberOfGroups: props.phase.groups?.length ?? 1,
        groupWinners: props.phase.groupWinners ?? null,
        totalTeamsProceeding: props.phase.totalTeamsProceeding ?? null,
        startTime: props.phase.startTime ?? '',
      }
    } else {
      form.value = { name: '', format: 'RoundRobin', numberOfGroups: 2, groupWinners: null, totalTeamsProceeding: null, startTime: '' }
    }
  }
})

async function handleSave() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  try {
    const dto: PhaseDto = {
      name: form.value.name,
      format: form.value.format,
      numberOfGroups: props.phase ? undefined : form.value.numberOfGroups,
      groupWinners: form.value.groupWinners ?? undefined,
      totalTeamsProceeding: form.value.totalTeamsProceeding ?? undefined,
      startTime: form.value.startTime || undefined,
    }

    if (props.phase?.id) {
      await structureStore.updatePhase(props.tournamentId, props.phase.id, dto)
      showSuccess('Phase updated')
    } else {
      await structureStore.addPhase(props.tournamentId, dto)
      showSuccess('Phase created')
    }
    model.value = false
    emit('saved')
  } catch (error) {
    if (!handleError(error)) {
      showError(props.phase ? 'Failed to update phase' : 'Failed to create phase')
    }
  }
}
</script>

<template>
  <v-dialog v-model="model" max-width="500">
    <v-card class="pa-2">
      <v-card-title class="text-uppercase" style="letter-spacing: 1.5px">
        {{ phase ? 'Edit Phase' : 'Add Phase' }}
      </v-card-title>
      <v-card-text>
        <v-form ref="formRef" @submit.prevent="handleSave">
          <v-text-field
            v-model="form.name"
            label="Name"
            :rules="nameRules"
            :error-messages="fieldErrors('name')"
            @update:model-value="clearFieldError('name')"
          />
          <v-select
            v-model="form.format"
            label="Format"
            :items="PHASE_FORMATS"
            item-title="title"
            item-value="value"
            :error-messages="fieldErrors('format')"
            @update:model-value="clearFieldError('format')"
          />
          <v-text-field
            v-if="!phase"
            v-model.number="form.numberOfGroups"
            label="Number of Groups"
            type="number"
            :rules="groupsRules"
            min="1"
          />
          <v-text-field
            v-model.number="form.groupWinners"
            label="Teams Proceeding per Group"
            type="number"
            hint="Top N teams from each group advance automatically"
            persistent-hint
            min="1"
            clearable
          />
          <v-text-field
            v-model.number="form.totalTeamsProceeding"
            label="Total Teams Proceeding"
            type="number"
            hint="Total teams qualifying from this phase (includes lucky losers)"
            persistent-hint
            min="1"
            clearable
          />
          <v-text-field
            v-model="form.startTime"
            label="Start Time"
            type="time"
            hint="Baseline time for scheduling games in this phase"
            persistent-hint
            clearable
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="model = false">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" @click="handleSave">
          {{ phase ? 'Save' : 'Create' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
