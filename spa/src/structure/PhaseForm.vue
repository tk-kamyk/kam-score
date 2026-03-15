<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useStructureStore } from '@/structure/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import { PHASE_FORMATS } from '@/structure/types'
import type { PhaseDto } from '@/structure/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  phase: PhaseDto | null
  hasGames?: boolean
}>()

const model = defineModel<boolean>()

const emit = defineEmits<{
  saved: []
}>()

const structureStore = useStructureStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError, generalError } = useFormErrors()
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const useLevels = ref(false)

const form = ref({
  name: '',
  format: 'RoundRobin',
  numberOfGroups: 2,
  numberOfLevels: 2,
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

/** Level count of the previous phase (0 if no levels or no previous phase) */
const previousPhaseLevelCount = computed(() => {
  if (props.phase) return 0 // editing — levels are structural, can't change
  const phases = structureStore.structure?.phases ?? []
  if (phases.length === 0) return 0
  const lastPhase = phases[phases.length - 1]
  return lastPhase.levels?.length ?? 0
})

/** Whether the previous phase requires this phase to have levels */
const levelsRequired = computed(() => previousPhaseLevelCount.value > 0)

/** Minimum number of levels for this phase */
const minLevels = computed(() => levelsRequired.value ? previousPhaseLevelCount.value : 1)

const levelsRules = computed(() => [
  (v: number) => v >= minLevels.value || `At least ${minLevels.value} levels required (previous phase has ${previousPhaseLevelCount.value}).`,
  (v: number) => {
    if (previousPhaseLevelCount.value === 0) return true
    return v % previousPhaseLevelCount.value === 0
      || `Must be a multiple of ${previousPhaseLevelCount.value} (previous phase level count).`
  },
])

const groupsLabel = computed(() =>
  useLevels.value ? 'Groups per Level' : 'Number of Groups',
)

const totalGroupsHint = computed(() => {
  if (!useLevels.value) return ''
  const levels = form.value.numberOfLevels
  const groups = form.value.numberOfGroups
  if (levels >= 1 && groups >= 1) {
    return `${levels * groups} total groups (${groups} groups × ${levels} levels)`
  }
  return ''
})

const levelsHint = computed(() => {
  if (!levelsRequired.value) return 'Split groups into levels (e.g. Gold, Silver)'
  return `Previous phase has ${previousPhaseLevelCount.value} levels — must be a multiple`
})

const totalProceedingLabel = computed(() =>
  useLevels.value ? 'Total Teams proceeding per Level' : 'Total Teams proceeding',
)

watch(model, (open) => {
  if (open) {
    clearErrors()
    if (props.phase) {
      useLevels.value = (props.phase.levels?.length ?? 0) > 0
      form.value = {
        name: props.phase.name,
        format: props.phase.format,
        numberOfGroups: props.phase.groups?.length ?? 1,
        numberOfLevels: props.phase.levels?.length ?? 2,
        groupWinners: props.phase.groupWinners ?? null,
        totalTeamsProceeding: props.phase.totalTeamsProceeding ?? null,
        startTime: props.phase.startTime ?? '',
      }
    } else {
      useLevels.value = levelsRequired.value
      const defaultLevels = levelsRequired.value ? previousPhaseLevelCount.value : 2
      form.value = { name: '', format: 'RoundRobin', numberOfGroups: 2, numberOfLevels: defaultLevels, groupWinners: null, totalTeamsProceeding: null, startTime: '' }
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
      numberOfLevels: props.phase ? undefined : (useLevels.value ? form.value.numberOfLevels : undefined),
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
  <v-dialog v-model="model" max-width="500" aria-labelledby="phase-form-dialog-title">
    <v-card class="pa-2">
      <v-card-title id="phase-form-dialog-title" class="text-uppercase dialog-title">
        {{ phase ? 'Edit Phase' : 'Add Phase' }}
      </v-card-title>
      <v-card-text>
        <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
          {{ generalError }}
        </v-alert>
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
            :disabled="props.hasGames"
            :hint="props.hasGames ? 'Cannot change format while games exist' : undefined"
            :persistent-hint="!!props.hasGames"
            :error-messages="fieldErrors('format')"
            @update:model-value="clearFieldError('format')"
          />
          <v-switch
            v-if="!phase && !levelsRequired"
            v-model="useLevels"
            label="Use Levels"
            hint="Split groups into levels (e.g. Gold, Silver)"
            persistent-hint
            color="primary"
            density="compact"
            class="mb-2"
          />
          <v-text-field
            v-if="!phase && (useLevels || levelsRequired)"
            v-model.number="form.numberOfLevels"
            label="Number of Levels"
            type="number"
            :rules="levelsRules"
            :error-messages="fieldErrors('numberOfLevels')"
            :min="minLevels"
            :hint="levelsHint"
            persistent-hint
            @update:model-value="clearFieldError('numberOfLevels')"
          />
          <v-text-field
            v-if="!phase"
            v-model.number="form.numberOfGroups"
            :label="groupsLabel"
            type="number"
            :rules="groupsRules"
            :error-messages="fieldErrors('numberOfGroups')"
            :hint="totalGroupsHint"
            :persistent-hint="!!totalGroupsHint"
            min="1"
            @update:model-value="clearFieldError('numberOfGroups')"
          />
          <v-text-field
            v-model.number="form.groupWinners"
            label="Teams Proceeding per Group"
            type="number"
            hint="Top N teams from each group advance automatically"
            persistent-hint
            :error-messages="fieldErrors('groupWinners')"
            min="1"
            clearable
            @update:model-value="clearFieldError('groupWinners')"
          />
          <v-text-field
            v-model.number="form.totalTeamsProceeding"
            :label="totalProceedingLabel"
            type="number"
            hint="Total teams qualifying from this phase/level (includes lucky losers)"
            persistent-hint
            :error-messages="fieldErrors('totalTeamsProceeding')"
            min="1"
            clearable
            @update:model-value="clearFieldError('totalTeamsProceeding')"
          />
          <v-text-field
            v-model="form.startTime"
            label="Start Time"
            type="time"
            :hint="props.hasGames ? 'Cannot change start time while games exist' : 'Baseline time for scheduling games in this phase'"
            persistent-hint
            :error-messages="fieldErrors('startTime')"
            :disabled="props.hasGames"
            clearable
            @update:model-value="clearFieldError('startTime')"
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
