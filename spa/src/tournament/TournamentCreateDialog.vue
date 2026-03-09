<script setup lang="ts">
import { ref } from 'vue'
import type { TournamentDto } from '@/tournament/types'
import GameConditionsForm from '@/tournament/GameConditionsForm.vue'
import { buildGameConditions } from '@/tournament/gameConditionsUtils'

const model = defineModel<boolean>({ required: true })
const emit = defineEmits<{ created: [tournament: TournamentDto] }>()

const useCustomConditions = ref(false)
const bestOfSets = ref<number | undefined>()
const pointsPerSetText = ref('')
const newTournament = ref<TournamentDto>({
  name: '',
  discipline: 'Volleyball',
})

const disciplines = ['Volleyball', 'BeachVolleyball']

function handleCreate() {
  const dto: TournamentDto = {
    ...newTournament.value,
    gameConditions: buildGameConditions(useCustomConditions.value, bestOfSets.value, pointsPerSetText.value),
  }
  emit('created', dto)
  model.value = false
  newTournament.value = { name: '', discipline: 'Volleyball' }
  useCustomConditions.value = false
  bestOfSets.value = undefined
  pointsPerSetText.value = ''
}
</script>

<template>
  <v-dialog v-model="model" max-width="500" aria-labelledby="create-tournament-dialog-title">
    <v-card class="pa-2">
      <v-card-title id="create-tournament-dialog-title" class="text-uppercase dialog-title">Create Tournament</v-card-title>
      <v-card-text>
        <v-text-field
          v-model="newTournament.name"
          label="Name"
          autofocus
        />
        <v-select
          v-model="newTournament.discipline"
          :items="disciplines"
          label="Discipline"
        />
        <v-text-field
          v-model="newTournament.startTime"
          label="Date"
          type="date"
        />
        <v-text-field
          v-model.number="newTournament.gameLength"
          label="Game Length (minutes)"
          type="number"
        />
        <GameConditionsForm
          v-model:enabled="useCustomConditions"
          v-model:best-of-sets="bestOfSets"
          v-model:points-per-set-text="pointsPerSetText"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="model = false">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="elevated"
          :disabled="!newTournament.name"
          @click="handleCreate"
        >
          Create
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
