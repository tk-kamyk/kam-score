<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useTournamentStore } from '@/tournament/store'
import type { TournamentDto } from '@/tournament/types'
import GameConditionsForm from '@/tournament/GameConditionsForm.vue'
import { buildGameConditions } from '@/tournament/gameConditionsUtils'

const model = defineModel<boolean>({ required: true })
const props = defineProps<{ loading?: boolean }>()
const emit = defineEmits<{ created: [tournament: TournamentDto] }>()

const tournamentStore = useTournamentStore()

const useCustomConditions = ref(false)
const bestOfSets = ref<number | undefined>()
const pointsPerSetText = ref('')
const sourceTournamentId = ref<string | undefined>()
const newTournament = ref<TournamentDto>({
  name: '',
  discipline: 'Volleyball',
})

const disciplines = ['Volleyball', 'BeachVolleyball']

const tournamentOptions = computed(() =>
  [...tournamentStore.tournaments]
    .sort(
      (a, b) => new Date(b.lastModified ?? 0).getTime() - new Date(a.lastModified ?? 0).getTime(),
    )
    .map((t) => ({
      title: `${t.name} (${t.teamCount ?? 0} teams, ${t.courtCount ?? 0} courts)`,
      value: t.id!,
    })),
)

const selectedSource = computed(() =>
  sourceTournamentId.value
    ? tournamentStore.tournaments.find((t) => t.id === sourceTournamentId.value)
    : undefined,
)

function resetForm() {
  newTournament.value = { name: '', discipline: 'Volleyball' }
  useCustomConditions.value = false
  bestOfSets.value = undefined
  pointsPerSetText.value = ''
  sourceTournamentId.value = undefined
}

watch(model, (open) => {
  if (!open) resetForm()
})

function handleCreate() {
  const dto: TournamentDto = {
    ...newTournament.value,
    gameConditions: buildGameConditions(
      useCustomConditions.value,
      bestOfSets.value,
      pointsPerSetText.value,
    ),
    sourceTournamentId: sourceTournamentId.value,
  }
  emit('created', dto)
}
</script>

<template>
  <v-dialog v-model="model" max-width="500" aria-labelledby="create-tournament-dialog-title">
    <v-card class="pa-2">
      <v-card-title id="create-tournament-dialog-title" class="text-uppercase dialog-title"
        >Create Tournament</v-card-title
      >
      <v-progress-linear v-if="props.loading" indeterminate color="primary" />
      <v-card-text>
        <v-text-field v-model="newTournament.name" label="Name" autofocus />
        <v-select
          v-model="sourceTournamentId"
          :items="tournamentOptions"
          label="Copy structure from (optional)"
          clearable
          variant="outlined"
          density="comfortable"
          class="mb-2"
        />
        <template v-if="selectedSource">
          <v-alert type="info" variant="tonal" density="compact" class="mb-4">
            Copying from <strong>{{ selectedSource.name }}</strong
            >: {{ selectedSource.discipline }}, {{ selectedSource.teamCount ?? 0 }} seed teams,
            {{ selectedSource.courtCount ?? 0 }} courts,
            {{
              selectedSource.gameLength
                ? `${selectedSource.gameLength} min games`
                : 'no game length'
            }}. Settings below will be overridden by source.
          </v-alert>
        </template>
        <template v-else>
          <v-select
            v-model="newTournament.discipline"
            :items="disciplines"
            label="Discipline"
            :disabled="!!selectedSource"
          />
          <v-text-field
            v-model="newTournament.startTime"
            label="Date"
            type="date"
            :disabled="!!selectedSource"
          />
          <v-text-field
            v-model.number="newTournament.gameLength"
            label="Game Length (minutes)"
            type="number"
            :disabled="!!selectedSource"
          />
          <GameConditionsForm
            v-if="!selectedSource"
            v-model:enabled="useCustomConditions"
            v-model:best-of-sets="bestOfSets"
            v-model:points-per-set-text="pointsPerSetText"
          />
        </template>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="props.loading" @click="model = false">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="elevated"
          :disabled="!newTournament.name || props.loading"
          :loading="props.loading"
          @click="handleCreate"
        >
          Create
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
