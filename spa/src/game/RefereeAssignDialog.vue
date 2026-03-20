<script setup lang="ts">
import { ref, watch } from 'vue'
import type { GameDto } from '@/game/types'

export interface RefereeCandidateDto {
  teamId: string
  teamName: string
}

const props = defineProps<{
  modelValue: boolean
  game: GameDto
  tournamentId: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'assigned'): void
}>()

const candidates = ref<RefereeCandidateDto[]>([])
const selectedTeamId = ref<string | null>(null)
const loading = ref(false)
const submitting = ref(false)

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      selectedTeamId.value = null
      loadCandidates()
    }
  },
)

async function loadCandidates() {
  loading.value = true
  try {
    // TODO: Replace with real API call in Gate 6
    candidates.value = [
      { teamId: 'mock-1', teamName: 'Team C' },
      { teamId: 'mock-2', teamName: 'Team D' },
      { teamId: 'mock-3', teamName: 'Loser QF1' },
    ]
  } finally {
    loading.value = false
  }
}

function close() {
  emit('update:modelValue', false)
}

async function submit() {
  if (!selectedTeamId.value) return
  submitting.value = true
  try {
    // TODO: Replace with real API call in Gate 6
    emit('assigned')
    close()
  } finally {
    submitting.value = false
  }
}

function gameSummary(): string {
  const home = props.game.homeTeamName ?? props.game.homeTeamPlaceholder ?? 'Home'
  const away = props.game.awayTeamName ?? props.game.awayTeamPlaceholder ?? 'Away'
  return `${home} vs ${away}`
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="400"
    aria-labelledby="referee-assign-dialog-title"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="pa-2">
      <v-card-title id="referee-assign-dialog-title" class="text-uppercase dialog-title">
        Assign Referee
      </v-card-title>

      <v-card-text>
        <div class="text-body-medium text-medium-emphasis mb-4">
          {{ gameSummary() }}
        </div>

        <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-3" />

        <v-autocomplete
          v-if="!loading"
          v-model="selectedTeamId"
          :items="candidates"
          item-title="teamName"
          item-value="teamId"
          label="Select team"
          density="comfortable"
          hide-details
          auto-select-first
        />

        <div v-if="!loading && candidates.length === 0" class="text-body-medium text-medium-emphasis mt-2">
          No eligible teams available for this time slot.
        </div>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="close">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="elevated"
          :loading="submitting"
          :disabled="!selectedTeamId"
          @click="submit"
        >
          Assign
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
