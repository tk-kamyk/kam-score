<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFormErrors } from '@/composables/useFormErrors'
import { STATION_COUNT } from '@/volunteer/stations'

const props = defineProps<{
  modelValue: boolean
  shiftGroup: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  confirm: [payload: { volunteersPerShift: number; stationCount: number | null }]
}>()

const { generalError, clearErrors, handleError } = useFormErrors()

const volunteersPerShift = ref(2)
const stations = ref<number | null>(null)
const submitting = ref(false)

const volunteersPerShiftRules = [(v: number) => (v >= 1 && v <= 50) || 'Must be between 1 and 50.']
const stationsRules = [
  (v: number | null) =>
    v == null ||
    v === ('' as unknown) ||
    (v >= 1 && v <= STATION_COUNT) ||
    `Must be between 1 and ${STATION_COUNT}, or empty.`,
]

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    volunteersPerShift.value = 2
    stations.value = null
    submitting.value = false
    clearErrors()
  },
)

function submit() {
  if (volunteersPerShift.value < 1 || volunteersPerShift.value > 50) return
  const count = stations.value
  if (count != null && (count < 1 || count > STATION_COUNT)) return
  submitting.value = true
  emit('confirm', { volunteersPerShift: volunteersPerShift.value, stationCount: count ?? null })
}

defineExpose({ handleError, submitting })
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="450"
    aria-labelledby="auto-assign-dialog-title"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="pa-2">
      <v-card-title id="auto-assign-dialog-title" class="text-uppercase dialog-title">
        Auto-assign volunteers
      </v-card-title>
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
        <p class="text-body-2 text-medium-emphasis mb-3">
          Top up each shift in <strong>{{ shiftGroup }}</strong> to the selected number of
          volunteers. Existing assignments are preserved.
        </p>
        <v-text-field
          v-model.number="volunteersPerShift"
          label="Volunteers per shift"
          type="number"
          :min="1"
          :max="50"
          :rules="volunteersPerShiftRules"
          :disabled="submitting"
        />
        <v-text-field
          v-model.number="stations"
          label="Stations"
          type="number"
          clearable
          :min="1"
          :max="STATION_COUNT"
          :rules="stationsRules"
          :disabled="submitting"
          hint="Spreads volunteers in each shift evenly across stations, overwriting existing assignments."
          persistent-hint
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="submitting" @click="emit('update:modelValue', false)">
          Cancel
        </v-btn>
        <v-btn color="primary" variant="elevated" :loading="submitting" @click="submit">
          Auto-assign
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
