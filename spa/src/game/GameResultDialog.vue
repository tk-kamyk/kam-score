<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useGameStore } from '@/game/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import type { GameDto, SetResultDto } from '@/game/types'

const props = defineProps<{
  modelValue: boolean
  game: GameDto
  tournamentId: string
  isOwner: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const gameStore = useGameStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()
const { handleError, generalError, clearErrors } = useFormErrors()

const defaultSetCount = computed(() =>
  tournamentStore.currentTournament?.gameConditions?.bestOfSets ?? 1
)

const mode = ref<'detailed' | 'simple'>('detailed')
const tournamentCode = ref('')
const homeScore = ref<number>(0)
const awayScore = ref<number>(0)
const sets = ref<SetResultDto[]>([
  { homePoints: 0, awayPoints: 0 },
  { homePoints: 0, awayPoints: 0 },
])
const submitting = ref(false)

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      clearErrors()
      tournamentCode.value = ''
      if (props.game.sets?.length) {
        mode.value = 'detailed'
        sets.value = props.game.sets.map(s => ({ homePoints: s.homePoints, awayPoints: s.awayPoints }))
      } else if (props.game.homeScore != null) {
        mode.value = 'simple'
        homeScore.value = props.game.homeScore
        awayScore.value = props.game.awayScore ?? 0
      } else {
        mode.value = 'detailed'
        homeScore.value = 0
        awayScore.value = 0
        sets.value = Array.from({ length: defaultSetCount.value }, () => ({
          homePoints: 0,
          awayPoints: 0,
        }))
      }
    }
  },
  { immediate: true },
)

function addSet() {
  sets.value.push({ homePoints: 0, awayPoints: 0 })
}

function removeSet(index: number) {
  sets.value.splice(index, 1)
}

function close() {
  emit('update:modelValue', false)
}

async function submit() {
  submitting.value = true
  try {
    const code = props.isOwner ? undefined : tournamentCode.value.toUpperCase() || undefined
    if (mode.value === 'detailed') {
      const filledSets = sets.value.filter(
        s => s.homePoints !== 0 || s.awayPoints !== 0,
      )
      await gameStore.recordResult(
        props.tournamentId,
        props.game.id!,
        { sets: filledSets },
        code,
      )
    } else {
      await gameStore.recordResult(
        props.tournamentId,
        props.game.id!,
        { homeScore: homeScore.value, awayScore: awayScore.value },
        code,
      )
    }
    showSuccess('Result recorded')
    close()
  } catch (error) {
    if (!handleError(error)) {
      showError('Failed to record result. Check the tournament code and try again.')
    }
  } finally {
    submitting.value = false
  }
}

function homeName(): string {
  return props.game.homeTeamName ?? props.game.homeTeamPlaceholder ?? 'Home'
}

function awayName(): string {
  return props.game.awayTeamName ?? props.game.awayTeamPlaceholder ?? 'Away'
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="500" aria-labelledby="result-dialog-title" @update:model-value="emit('update:modelValue', $event)">
    <v-card class="pa-2">
      <v-card-title id="result-dialog-title" class="text-uppercase dialog-title">
        {{ game.status === 'Completed' ? 'Edit Result' : 'Enter Result' }}
      </v-card-title>

      <v-card-text>
        <v-alert v-if="generalError" type="error" variant="tonal" density="compact" closable role="alert" class="mb-3" @click:close="clearErrors()">
          {{ generalError }}
        </v-alert>
        <div class="text-center mb-4">
          <div class="text-body-medium text-medium-emphasis mb-4">
            {{ homeName() }} vs {{ awayName() }}
          </div>

          <v-btn-toggle v-model="mode" mandatory variant="outlined" density="comfortable" class="mb-4" aria-label="Result entry mode">
            <v-btn value="detailed">Detailed</v-btn>
            <v-btn value="simple">Simple</v-btn>
          </v-btn-toggle>
        </div>

        <template v-if="mode === 'detailed'">
          <div class="d-flex text-body-small text-medium-emphasis mb-1 ga-2">
            <span class="flex-1-1">Set</span>
            <span class="set-column text-center">{{ homeName() }}</span>
            <span class="set-column text-center">{{ awayName() }}</span>
            <span class="remove-btn-column" />
          </div>
          <div v-for="(set, i) in sets" :key="i" class="d-flex align-center ga-2 mb-2">
            <span class="text-body-medium text-medium-emphasis flex-1-1">Set {{ i + 1 }}</span>
            <v-text-field
              v-model.number="set.homePoints"
              type="number"
              density="comfortable"
              hide-details
              min="0"
              :aria-label="homeName() + ' points, set ' + (i + 1)"
              class="set-input"
            />
            <v-text-field
              v-model.number="set.awayPoints"
              type="number"
              density="comfortable"
              hide-details
              min="0"
              :aria-label="awayName() + ' points, set ' + (i + 1)"
              class="set-input"
            />
            <v-btn
              v-if="sets.length > 1"
              icon="mdi-close"
              size="x-small"
              variant="text"
              color="error"
              :aria-label="'Remove set ' + (i + 1)"
              @click="removeSet(i)"
            />
            <span v-else class="remove-btn-spacer" />
          </div>
          <v-btn
            prepend-icon="mdi-plus"
            variant="text"
            size="small"
            class="mt-1"
            @click="addSet"
          >
            Add Set
          </v-btn>
        </template>

        <template v-else>
          <div class="d-flex gap-4 align-center">
            <v-text-field
              v-model.number="homeScore"
              :label="`${homeName()} sets won`"
              type="number"
              density="compact"
              min="0"
              hide-details
            />
            <span class="text-headline-small">–</span>
            <v-text-field
              v-model.number="awayScore"
              :label="`${awayName()} sets won`"
              type="number"
              density="compact"
              min="0"
              hide-details
            />
          </div>
        </template>

        <v-text-field
          v-if="!isOwner"
          v-model="tournamentCode"
          label="Tournament Code"
          placeholder="e.g. A3F2"
          density="compact"
          class="mt-4"
          maxlength="4"
          :rules="[(v: string) => v.length === 4 || 'Enter a 4-character code']"
        />
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="close">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="elevated"
          :loading="submitting"
          @click="submit"
        >
          Save Result
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.set-column {
  width: 80px;
}

.set-input {
  max-width: 80px;
}

.remove-btn-column {
  width: 36px;
}

.remove-btn-spacer {
  width: 28px;
}
</style>
