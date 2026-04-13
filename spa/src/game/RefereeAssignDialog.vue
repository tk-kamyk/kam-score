<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useGameStore } from '@/game/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { parseErrorDetail } from '@/api/errors'
import type { GameDto, RefereeCandidateDto } from '@/game/types'

const props = defineProps<{
  modelValue: boolean
  game: GameDto
  tournamentId: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'assigned'): void
}>()

const gameStore = useGameStore()
const { showSuccess, showError } = useSnackbar()

const candidates = ref<RefereeCandidateDto[]>([])
const selectedTeamId = ref<string | null>(null)
const loading = ref(false)
const submitting = ref(false)
const isReassignment = computed(
  () => !!props.game.refereeTeamId || !!props.game.refereeTeamPlaceholder,
)

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      selectedTeamId.value = props.game.refereeTeamId ?? null
      loadCandidates()
    }
  },
  { immediate: true },
)

async function loadCandidates() {
  loading.value = true
  try {
    const result = await gameStore.fetchRefereeCandidates(props.tournamentId, props.game.id!)
    candidates.value = [...result].sort((a, b) => {
      if (a.isPlaceholder === b.isPlaceholder) return 0
      return a.isPlaceholder ? -1 : 1
    })
  } catch (error) {
    showError(parseErrorDetail(error) ?? 'Failed to load referee candidates')
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
    const selected = candidates.value.find((c) => c.teamId === selectedTeamId.value)
    await gameStore.assignReferee(
      props.tournamentId,
      props.game.id!,
      selectedTeamId.value,
      selected?.isPlaceholder,
    )
    showSuccess(isReassignment.value ? 'Referee reassigned' : 'Referee assigned')
    emit('assigned')
    close()
  } catch (error) {
    showError(parseErrorDetail(error) ?? 'Failed to assign referee')
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
        {{ isReassignment ? 'Reassign Referee' : 'Assign Referee' }}
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

        <div
          v-if="!loading && candidates.length === 0"
          class="text-body-medium text-medium-emphasis mt-2"
        >
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
