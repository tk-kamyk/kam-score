<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFormErrors } from '@/composables/useFormErrors'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  modelValue: boolean
  titleId: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  add: [name: string]
}>()

const { fieldErrors, clearErrors, clearFieldError, generalError, handleError } = useFormErrors()

const newGroupName = ref('')
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const groupNameRules = [
  (v: string) => !!v || 'Group name is required.',
  (v: string) => v.length <= 100 || 'Group name must not exceed 100 characters.',
]

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    newGroupName.value = ''
    clearErrors()
  },
)

async function submit() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return
  emit('add', newGroupName.value)
}

defineExpose({ handleError })
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="400"
    :aria-labelledby="titleId"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="pa-2">
      <v-card-title :id="titleId" class="text-uppercase dialog-title">Add Group</v-card-title>
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
            v-model="newGroupName"
            label="Group Name"
            :rules="groupNameRules"
            :error-messages="fieldErrors('name')"
            @update:model-value="clearFieldError('name')"
          />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" @click="submit">Add</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
