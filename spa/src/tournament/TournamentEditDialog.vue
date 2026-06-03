<script setup lang="ts">
import { ref, watch } from 'vue'
import GameConditionsForm from '@/tournament/GameConditionsForm.vue'
import { buildGameConditions, formatPointsPerSet } from '@/tournament/gameConditionsUtils'
import { TOURNAMENT_TYPES, type TournamentDto } from '@/tournament/types'

const model = defineModel<boolean>({ required: true })
const props = defineProps<{ tournament: TournamentDto }>()
const emit = defineEmits<{ updated: [dto: TournamentDto] }>()

const editForm = ref<TournamentDto>({ name: '', discipline: 'Volleyball' })
const customConditions = ref(false)
const bestOfSets = ref<number | undefined>()
const pointsPerSetText = ref('')

const disciplines = ['Volleyball', 'BeachVolleyball']

watch(model, (open) => {
  if (!open) return
  editForm.value = {
    ...props.tournament,
    startTime: props.tournament.startTime?.split('T')[0],
  }
  customConditions.value = !!props.tournament.gameConditions
  bestOfSets.value = props.tournament.gameConditions?.bestOfSets
  pointsPerSetText.value = formatPointsPerSet(props.tournament.gameConditions?.pointsPerSet)
})

function handleUpdate() {
  const dto: TournamentDto = {
    ...editForm.value,
    gameConditions: buildGameConditions(
      customConditions.value,
      bestOfSets.value,
      pointsPerSetText.value,
    ),
  }
  emit('updated', dto)
  model.value = false
}
</script>

<template>
  <v-dialog v-model="model" max-width="500" aria-labelledby="edit-tournament-dialog-title">
    <v-card class="pa-2">
      <v-card-title id="edit-tournament-dialog-title" class="text-uppercase dialog-title"
        >Edit Tournament</v-card-title
      >
      <v-card-text>
        <v-text-field v-model="editForm.name" label="Name" />
        <v-select v-model="editForm.discipline" :items="disciplines" label="Discipline" />
        <v-select v-model="editForm.type" :items="TOURNAMENT_TYPES" label="Visibility" />
        <v-text-field v-model="editForm.startTime" label="Date" type="date" />
        <v-text-field
          v-model.number="editForm.gameLength"
          label="Game Length (minutes)"
          type="number"
        />
        <GameConditionsForm
          v-model:enabled="customConditions"
          v-model:best-of-sets="bestOfSets"
          v-model:points-per-set-text="pointsPerSetText"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="model = false">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" :disabled="!editForm.name" @click="handleUpdate">
          Save
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
