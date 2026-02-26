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
      <h1 class="hero-title text-uppercase mb-2">Tournament Management</h1>
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
          <v-card-title class="text-uppercase" style="letter-spacing: 1px;">
            {{ tournament.name }}
          </v-card-title>
          <v-card-subtitle class="mt-1">{{ tournament.discipline }}</v-card-subtitle>
          <v-card-text class="pt-3 d-flex flex-row justify-space-between">
            <div>
              <v-chip v-if="tournament.gameLength" size="small" variant="outlined" class="mr-2" prepend-icon="mdi-clock">
                {{ tournament.gameLength }} min
              </v-chip>
              <v-chip size="small" variant="outlined" prepend-icon="mdi-account-group-outline" class="mr-2">
                {{ tournament.teamCount ?? 0 }} teams
              </v-chip>
              <v-chip size="small" variant="outlined" prepend-icon="mdi-volleyball">
                {{ tournament.courtCount ?? 0 }} courts
              </v-chip>
            </div>
            <div>
              <v-chip v-if="tournament.tournamentCode" size="small" prepend-icon="mdi-key" color="secondary" variant="tonal" class="mr-2 font-weight-bold" style="letter-spacing: 1px;">
                {{ tournament.tournamentCode }}
              </v-chip>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-card v-if="!tournamentStore.loading && tournamentStore.tournaments.length === 0" class="pa-12 text-center">
      <v-icon size="72" color="secondary" class="mb-4" style="opacity: 0.6;">mdi-trophy-outline</v-icon>
      <p class="text-h6 text-uppercase mb-2" style="letter-spacing: 1.5px; opacity: 0.5;">No tournaments yet</p>
      <p v-if="auth.isAuthenticated" class="text-body-2" style="opacity: 0.4;">Click the button above to create your first tournament.</p>
    </v-card>

    <v-dialog v-model="showCreateDialog" max-width="500">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase" style="letter-spacing: 1.5px;">Create Tournament</v-card-title>
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
    font-size: 1.75rem;
}

@media (min-width: 960px) {
    .hero-title {
        font-size: 2.25rem;
    }
}

.hero-subtitle {
    opacity: 0.5;
    font-size: 1rem;
}

.tournament-card {
    border: 1px solid rgba(var(--ks-surface), 0.5);
    transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.tournament-card:hover {
    border-color: rgba(var(--ks-primary), 0.4);
    box-shadow: 0 0 20px rgba(var(--ks-primary), 0.08);
}
</style>
