<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useFormErrors } from '@/composables/useFormErrors'

const props = defineProps<{
  modelValue: boolean
  existingTeamCount: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  generate: [count: number]
}>()

const { generalError, clearErrors, handleError } = useFormErrors()

const seedCount = ref(8)

const seedCountRules = [(v: number) => (v >= 1 && v <= 100) || 'Count must be between 1 and 100.']

const seedPreview = computed(() => {
  const start = props.existingTeamCount + 1
  const count = seedCount.value
  if (count < 1 || count > 100) return ''
  const names = Array.from({ length: Math.min(count, 4) }, (_, i) => `Seed ${start + i}`)
  if (count > 4) names.push(`... Seed ${start + count - 1}`)
  return names.join(', ')
})

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    seedCount.value = 8
    clearErrors()
  },
)

function submit() {
  if (seedCount.value < 1 || seedCount.value > 100) return
  emit('generate', seedCount.value)
}

defineExpose({ handleError })
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="450"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">Generate Seed Teams</v-card-title>
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
        <v-text-field
          v-model.number="seedCount"
          label="Number of teams"
          type="number"
          :min="1"
          :max="100"
          :rules="seedCountRules"
        />
        <div v-if="seedPreview" class="text-body-2 text-medium-emphasis mt-1">
          Will generate: {{ seedPreview }}
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" @click="submit">Generate</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
