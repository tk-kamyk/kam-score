<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFormErrors } from '@/composables/useFormErrors'
import type { TeamDto } from '@/team/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  modelValue: boolean
  editingTeam: TeamDto | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  save: [team: TeamDto]
}>()

const { fieldErrors, clearErrors, clearFieldError, generalError, handleError } = useFormErrors()

const form = ref<TeamDto>({ name: '', level: 50 })
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const nameRules = [
  (v: string) => !!v || 'Team name is required.',
  (v: string) => v.length <= 100 || 'Team name must not exceed 100 characters.',
]
const emailRules = [
  (v: string | null | undefined) =>
    !v || /.+@.+\..+/.test(v) || 'Email must be a valid email address.',
]
const phoneRules = [
  (v: string | null | undefined) =>
    !v || /^\+?[\d\s\-()]{7,20}$/.test(v) || 'Phone must be a valid phone number.',
]

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    clearErrors()
    form.value = props.editingTeam
      ? { ...props.editingTeam }
      : { name: '', level: 50, email: null, phone: null }
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
        {{ editingTeam ? 'Edit Team' : 'Add Team' }}
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
          <v-slider
            v-model="form.level"
            label="Level"
            :min="0"
            :max="100"
            :step="1"
            thumb-label="always"
            color="primary"
            class="mt-4"
          />
          <v-text-field
            v-model="form.email"
            label="Email"
            type="email"
            :rules="emailRules"
            :error-messages="fieldErrors('email')"
            @update:model-value="clearFieldError('email')"
          />
          <v-text-field
            v-model="form.phone"
            label="Phone"
            :rules="phoneRules"
            :error-messages="fieldErrors('phone')"
            @update:model-value="clearFieldError('phone')"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" @click="submit">
          {{ editingTeam ? 'Save' : 'Create' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
