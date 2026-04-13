<script setup lang="ts">
import { useFormErrors } from '@/composables/useFormErrors'

defineProps<{
  modelValue: boolean
  title: string
  message: string
  confirmLabel?: string
  confirmColor?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  confirm: []
}>()

const { generalError, clearErrors, handleError } = useFormErrors()

defineExpose({ handleError })
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="420"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">{{ title }}</v-card-title>
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
        {{ message }}
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn :color="confirmColor ?? 'primary'" variant="elevated" @click="emit('confirm')">
          {{ confirmLabel ?? 'Confirm' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
