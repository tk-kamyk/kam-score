<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFormErrors } from '@/composables/useFormErrors'
import type { CourtDto } from '@/court/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  modelValue: boolean
  editingCourt: CourtDto | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  save: [court: CourtDto]
}>()

const { fieldErrors, clearErrors, clearFieldError, generalError, handleError } = useFormErrors()

const form = ref<CourtDto>({ name: '' })
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const nameRules = [
  (v: string) => !!v || 'Court name is required.',
  (v: string) => v.length <= 100 || 'Court name must not exceed 100 characters.',
]

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    clearErrors()
    form.value = props.editingCourt ? { ...props.editingCourt } : { name: '' }
  },
)

async function submit() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return
  emit('save', form.value)
}

defineExpose({ handleError })
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="500"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">
        {{ editingCourt ? 'Edit Court' : 'Add Court' }}
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
        <v-form ref="formRef" @submit.prevent="submit">
          <v-text-field
            v-model="form.name"
            label="Name"
            :rules="nameRules"
            :error-messages="fieldErrors('name')"
            @update:model-value="clearFieldError('name')"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" @click="submit">
          {{ editingCourt ? 'Save' : 'Create' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
