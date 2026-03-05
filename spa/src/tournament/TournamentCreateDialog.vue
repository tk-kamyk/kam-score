<script setup lang="ts">
import { ref, watch } from 'vue'
import type { TournamentDto } from '@/tournament/types'

const model = defineModel<boolean>({ required: true })
const emit = defineEmits<{ created: [tournament: TournamentDto] }>()

const useCustomConditions = ref(false)
const pointsPerSetText = ref('')
const newTournament = ref<TournamentDto>({
  name: '',
  discipline: 'Volleyball',
})

const disciplines = ['Volleyball', 'BeachVolleyball']

watch(useCustomConditions, (on) => {
  if (on) {
    newTournament.value.gameConditions = { bestOfSets: undefined, pointsPerSet: undefined }
  }
})

function handleCreate() {
  const dto = { ...newTournament.value }
  if (useCustomConditions.value) {
    const points = pointsPerSetText.value
      .split(',')
      .map(s => parseInt(s.trim()))
      .filter(n => !isNaN(n))
    dto.gameConditions = {
      bestOfSets: dto.gameConditions?.bestOfSets,
      pointsPerSet: points.length > 0 ? points : undefined,
    }
  } else {
    dto.gameConditions = undefined
  }
  emit('created', dto)
  model.value = false
  newTournament.value = { name: '', discipline: 'Volleyball' }
  useCustomConditions.value = false
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
          label="Start Time"
          type="datetime-local"
        />
        <v-text-field
          v-model.number="newTournament.gameLength"
          label="Game Length (minutes)"
          type="number"
        />
        <v-switch
          v-model="useCustomConditions"
          label="Custom game conditions"
          color="primary"
          density="comfortable"
          hide-details
          class="mb-4"
        />
        <template v-if="useCustomConditions">
          <v-select
            v-model="newTournament.gameConditions!.bestOfSets"
            :items="[1, 3, 5]"
            label="Best of Sets"
          />
          <v-text-field
            v-model="pointsPerSetText"
            label="Points per Set (comma-separated)"
            placeholder="25, 25, 15"
          />
        </template>
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
