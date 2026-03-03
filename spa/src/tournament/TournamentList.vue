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
    <div class="hero-section text-center mb-10">
      <img src="/volleyball.svg" alt="Volleyball" class="hero-icon mb-4" />
      <h1 class="hero-title text-title-large text-md-headline-large text-uppercase mb-2">Tournament Management</h1>
      <p class="hero-subtitle mb-6">Organize, schedule, and track your volleyball tournaments</p>
      <v-btn
        v-if="auth.isAuthenticated"
        color="primary"
        size="large"
        prepend-icon="mdi-plus"
        @click="showCreateDialog = true"
      >
        New Tournament
      </v-btn>
    </div>

    <v-progress-linear v-if="tournamentStore.loading" indeterminate color="primary" class="mb-4" />

    <v-row>
      <v-col
        v-for="tournament in tournamentStore.tournaments"
        :key="tournament.id"
        cols="12"
        md="6"
        lg="4"
      >
        <v-card
          class="tournament-card h-100 pa-2"
          @click="navigateToTournament(tournament.id!)"
          hover
        >
          <v-card-title class="text-uppercase tournament-card-title">
            {{ tournament.name }}
          </v-card-title>
          <v-card-subtitle class="mt-1">{{ tournament.discipline }}</v-card-subtitle>
          <v-card-text class="pt-3 d-flex flex-row justify-space-between">
            <div class="d-flex flex-wrap ga-1">
              <v-chip v-if="tournament.gameLength" size="small" variant="outlined" prepend-icon="mdi-clock">
                {{ tournament.gameLength }} min
              </v-chip>
              <v-chip size="small" variant="outlined" prepend-icon="mdi-account-group-outline">
                {{ tournament.teamCount ?? 0 }} teams
              </v-chip>
              <v-chip size="small" variant="outlined" prepend-icon="mdi-volleyball">
                {{ tournament.courtCount ?? 0 }} courts
              </v-chip>
            </div>
            <div>
              <v-chip v-if="tournament.tournamentCode" size="small" prepend-icon="mdi-key" color="secondary" variant="tonal" class="font-weight-bold code-chip">
                {{ tournament.tournamentCode }}
              </v-chip>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-card v-if="!tournamentStore.loading && tournamentStore.tournaments.length === 0" class="pa-12 text-center">
      <v-icon size="72" color="secondary" class="mb-4 empty-icon">mdi-trophy-outline</v-icon>
      <p class="text-title-medium text-sm-headline-small text-uppercase mb-2 dialog-title empty-heading">No tournaments yet</p>
      <p v-if="auth.isAuthenticated" class="text-body-medium empty-hint">Click the button above to create your first tournament.</p>
    </v-card>

    <v-dialog v-model="showCreateDialog" max-width="500">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Create Tournament</v-card-title>
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

<style scoped>
.hero-section {
    padding-top: 24px;
}

.hero-icon {
    width: 80px;
    height: 80px;
}

.hero-title {
    font-weight: 700;
    letter-spacing: 2px;
}

.hero-subtitle {
    opacity: 0.5;
    font-size: 1rem;
}

.tournament-card {
    border: 1px solid var(--ks-border);
    transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.tournament-card:hover {
    border-color: var(--ks-primary-border);
    box-shadow: 0 0 20px var(--ks-primary-glow);
}

.tournament-card-title {
    letter-spacing: 1px;
}

.empty-icon {
    opacity: 0.6;
}

.empty-heading {
    opacity: 0.5;
}

.empty-hint {
    opacity: 0.4;
}
</style>
