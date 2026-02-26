<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import type { TournamentDto } from '@/tournament/types'

const router = useRouter()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()

const showCreateDialog = ref(false)
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

onMounted(() => {
  tournamentStore.fetchTournaments()
})

async function handleCreate() {
  try {
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
    const created = await tournamentStore.createTournament(dto)
    showCreateDialog.value = false
    newTournament.value = { name: '', discipline: 'Volleyball' }
    useCustomConditions.value = false
    pointsPerSetText.value = ''
    showSuccess('Tournament created')
    router.push({ name: 'tournament', params: { id: created.id } })
  } catch {
    showError('Failed to create tournament')
  }
}

function navigateToTournament(id: string) {
  router.push({ name: 'tournament', params: { id } })
}
</script>

<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <h1 class="text-h4">Tournaments</h1>
      <v-btn
        v-if="auth.isAuthenticated"
        color="primary"
        icon="mdi-plus"
        @click="showCreateDialog = true"
      />
    </div>

    <v-progress-linear v-if="tournamentStore.loading" indeterminate color="primary" />

    <v-row>
      <v-col
        v-for="tournament in tournamentStore.tournaments"
        :key="tournament.id"
        cols="12"
        md="6"
        lg="4"
      >
        <v-card
          class="cursor-pointer h-100"
          @click="navigateToTournament(tournament.id!)"
          hover
        >
          <v-card-title>{{ tournament.name }}</v-card-title>
          <v-card-subtitle>{{ tournament.discipline }}</v-card-subtitle>
          <v-card-text>
            <v-chip v-if="tournament.tournamentCode" size="small" color="primary" class="mr-2">
              Code: {{ tournament.tournamentCode }}
            </v-chip>
            <v-chip v-if="tournament.gameLength" size="small" variant="outlined">
              {{ tournament.gameLength }} min
            </v-chip>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-card v-if="!tournamentStore.loading && tournamentStore.tournaments.length === 0" class="pa-8 text-center">
      <v-icon size="64" color="grey-lighten-1">mdi-trophy-outline</v-icon>
      <p class="text-h6 mt-4 text-grey">No tournaments yet</p>
    </v-card>

    <v-dialog v-model="showCreateDialog" max-width="500">
      <v-card title="Create Tournament">
        <v-card-text>
          <v-text-field
            v-model="newTournament.name"
            label="Name"
            variant="outlined"
            density="comfortable"
            autofocus
          />
          <v-select
            v-model="newTournament.discipline"
            :items="disciplines"
            label="Discipline"
            variant="outlined"
            density="comfortable"
          />
          <v-text-field
            v-model="newTournament.startTime"
            label="Start Time"
            type="datetime-local"
            variant="outlined"
            density="comfortable"
          />
          <v-text-field
            v-model.number="newTournament.gameLength"
            label="Game Length (minutes)"
            type="number"
            variant="outlined"
            density="comfortable"
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
              variant="outlined"
              density="comfortable"
            />
            <v-text-field
              v-model="pointsPerSetText"
              label="Points per Set (comma-separated)"
              placeholder="25, 25, 15"
              variant="outlined"
              density="comfortable"
            />
          </template>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showCreateDialog = false">Cancel</v-btn>
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
  </div>
</template>
