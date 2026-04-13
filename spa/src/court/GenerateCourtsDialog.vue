<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useFormErrors } from '@/composables/useFormErrors'

const props = defineProps<{
  modelValue: boolean
  existingCourtCount: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  generate: [count: number]
}>()

const { generalError, clearErrors, handleError } = useFormErrors()

const courtCount = ref(4)

const courtCountRules = [(v: number) => (v >= 1 && v <= 20) || 'Count must be between 1 and 20.']

const courtPreview = computed(() => {
  const start = props.existingCourtCount + 1
  const count = courtCount.value
  if (count < 1 || count > 20) return ''
  const names = Array.from({ length: Math.min(count, 4) }, (_, i) => `C${start + i}`)
  if (count > 4) names.push(`... C${start + count - 1}`)
  return names.join(', ')
})

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    courtCount.value = 4
    clearErrors()
  },
)

function submit() {
  if (courtCount.value < 1 || courtCount.value > 20) return
  emit('generate', courtCount.value)
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
      <v-card-title class="text-uppercase dialog-title">Generate Courts</v-card-title>
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
          v-model.number="courtCount"
          label="Number of courts"
          type="number"
          :min="1"
          :max="20"
          :rules="courtCountRules"
        />
        <div v-if="courtPreview" class="text-body-2 text-medium-emphasis mt-1">
          Will generate: {{ courtPreview }}
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
